# Design: Movimentações via Fila RabbitMQ

**Data:** 2026-09-24  
**Status:** Aprovado  
**Contexto:** Adicionar canal assíncrono via RabbitMQ para registrar créditos e débitos, tornando o sistema resiliente a picos de demanda. O endpoint HTTP síncrono existente é preservado (dual path).

---

## 1. Visão Geral

```
[Produtor API]  →  RabbitMQ (exchange: movimentacoes)
                        │
                        ▼
                [movimentacoes.registrar]   ← fila principal (durable)
                        │
              ConsumidorDeMovimentacoes
              (BackgroundService — Worker)
                        │
             ┌──────────┴──────────┐
             ▼                     ▼
     Sucesso: BasicAck      3 falhas Polly: BasicNack(requeue:false)
          │                             │
   PostgreSQL (via MediatR)     [movimentacoes.registrar.dlq]
                                  (inspeção manual)

[API REST existente] → direto → PostgreSQL   (caminho síncrono, inalterado)
```

**Premissas:**
- O caminho HTTP síncrono (`POST /contas/{id}/movimentacoes`) não é alterado.
- A fila é um canal alternativo — sistemas batch, integrações externas.
- O Worker reutiliza o `RegistrarMovimentacaoCommand` e todo o pipeline existente (validação, idempotência, retry de banco, métricas).
- A `ChaveIdempotencia` viaja na mensagem; o handler garante deduplicação mesmo com retries.

---

## 2. Novos Projetos

| Projeto | Tipo | Referências |
|---|---|---|
| `MovimentacoesFinanceiras.Mensagens` | classlib | — |
| `MovimentacoesFinanceiras.Worker` | Worker Service | `Mensagens`, `Aplicacao`, `Infraestrutura` |
| `MovimentacoesFinanceiras.Produtor` | ASP.NET Core mínimo | `Mensagens` apenas |

A `Api` existente **não referencia** `Mensagens` — não tem dependência de RabbitMQ.

---

## 3. Contrato de Mensagem

**Projeto:** `MovimentacoesFinanceiras.Mensagens`

```csharp
public record ComandoMovimentacaoMessage(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string? ChaveIdempotencia
);
```

Serializado como JSON via `System.Text.Json`. O Produtor gera a `ChaveIdempotencia` (novo `Guid`) na publicação.

**Constantes de topologia (mesmo namespace):**

```csharp
public static class TopologiaRabbitMq
{
    public const string Exchange      = "movimentacoes";
    public const string ExchangeDlx   = "movimentacoes.dlx";
    public const string FilaPrincipal = "movimentacoes.registrar";
    public const string FilaDlq       = "movimentacoes.registrar.dlq";
    public const string RoutingKey    = "registrar";
    public const string RoutingKeyDlq = "registrar.dlq";
}
```

---

## 4. Topologia RabbitMQ

Declarada no startup do Worker via `IHostedService` (`TopologiaDeclarator`):

```
Exchange "movimentacoes"    (direct, durable)
  └── binding → "movimentacoes.registrar"  (routing key: "registrar")

Fila "movimentacoes.registrar"  (durable)
  x-dead-letter-exchange:     "movimentacoes.dlx"
  x-dead-letter-routing-key:  "registrar.dlq"

Exchange "movimentacoes.dlx"  (direct, durable)
  └── binding → "movimentacoes.registrar.dlq"  (routing key: "registrar.dlq")

Fila "movimentacoes.registrar.dlq"  (durable)
```

Mensagens publicadas com `Persistent = true` — sobrevivem a restart do broker.

---

## 5. Worker — `MovimentacoesFinanceiras.Worker`

### Estrutura interna

```
Worker/
  Mensageria/
    ConexaoRabbitMq.cs          — IConnection singleton, reconexão com backoff
    TopologiaDeclarator.cs      — declara exchanges/filas no startup (IHostedService)
    ConsumidorDeMovimentacoes.cs — BackgroundService principal
  Processamento/
    ProcessadorDeMovimentacao.cs — converte mensagem → command → MediatR
  DependencyInjection.cs
  Program.cs
```

### Fluxo do `ConsumidorDeMovimentacoes`

1. Abre channel, configura `basicQos(prefetchCount: 1)`
2. Registra `AsyncEventingBasicConsumer`
3. Para cada mensagem:
   - Desserializa `ComandoMovimentacaoMessage`
   - Chama `ProcessadorDeMovimentacao.ProcessarAsync()`
   - Sucesso → `BasicAck`
   - `SaldoInsuficienteException` / `ContaNaoEncontradaException` → log + `BasicNack(requeue: false)` (erro permanente, sem retry)
   - Erro de infraestrutura → Polly (3 tentativas, backoff 2s/4s/8s) → se esgotar → `BasicNack(requeue: false)` → DLQ

### `ProcessadorDeMovimentacao`

```csharp
var command = new RegistrarMovimentacaoCommand(
    msg.ContaId, msg.Tipo, msg.Valor, msg.Descricao, msg.ChaveIdempotencia);
await mediator.Send(command, ct);
```

Reutiliza exatamente o mesmo pipeline da API: validação FluentValidation, idempotência, UPDATE atômico, Polly de banco, métricas OpenTelemetry.

### Resiliência do canal

O `ConsumidorDeMovimentacoes` monitora o evento `Shutdown` da conexão e reconecta com backoff exponencial — o Worker não morre se o broker reiniciar temporariamente.

---

## 6. Produtor — `MovimentacoesFinanceiras.Produtor`

### Estrutura interna

```
Produtor/
  Mensageria/
    IPublicadorDeMovimentacoes.cs
    PublicadorRabbitMq.cs        — IModel, serializa e publica
  Program.cs                     — Minimal API
  appsettings.json
```

### Endpoint

```
POST /movimentacoes
Body:     { "contaId": guid, "tipo": "Credito|Debito", "valor": decimal, "descricao": string? }
Response: 202 Accepted + { "chaveIdempotencia": "<guid-gerado>" }
```

Sem autenticação (serviço interno de teste). Sem referência ao domínio — só conhece `ComandoMovimentacaoMessage`.

### Publicação

```csharp
var props = channel.CreateBasicProperties();
props.Persistent    = true;
props.MessageId     = chaveIdempotencia;   // usado para rastreabilidade
props.ContentType   = "application/json";

channel.BasicPublish(
    exchange:        TopologiaRabbitMq.Exchange,
    routingKey:      TopologiaRabbitMq.RoutingKey,
    basicProperties: props,
    body:            JsonSerializer.SerializeToUtf8Bytes(mensagem));
```

---

## 7. docker-compose

Três novos serviços adicionados ao `docker-compose.yml` existente:

```yaml
rabbitmq:
  image: rabbitmq:3-management
  ports:
    - "5672:5672"
    - "15672:15672"    # management UI: http://localhost:15672 (guest/guest)
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5

worker:
  build:
    context: .
    dockerfile: src/MovimentacoesFinanceiras.Worker/Dockerfile
  depends_on:
    rabbitmq: { condition: service_healthy }
    postgres:  { condition: service_healthy }
  environment:
    RabbitMq__Host:              rabbitmq
    ConnectionStrings__Postgres: "Host=postgres;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres"

produtor:
  build:
    context: .
    dockerfile: src/MovimentacoesFinanceiras.Produtor/Dockerfile
  depends_on:
    rabbitmq: { condition: service_healthy }
  ports:
    - "8081:8080"
  environment:
    RabbitMq__Host: rabbitmq
```

A `api` existente **não ganha** dependência de RabbitMQ no compose.

---

## 8. Testes

### Worker

- **Unitários** (`ProcessadorDeMovimentacao`): mock de `IMediator`, verifica command correto, que `SaldoInsuficienteException` não aciona retry, que erros de infra ativam Polly.
- **Integração**: sobe `ConsumidorDeMovimentacoes` com RabbitMQ via Testcontainers, publica mensagem, verifica persistência no banco.

### Produtor

- **Unitário** (`PublicadorRabbitMq`): mock de `IModel`, verifica `BasicPublish` com exchange, routing key, `Persistent=true` e `MessageId` preenchido.

### Fluxo end-to-end (demonstração manual)

```bash
docker compose up --build
# POST no Produtor
curl -X POST http://localhost:8081/movimentacoes \
  -H "Content-Type: application/json" \
  -d '{"contaId":"<guid>","tipo":"Credito","valor":100.00}'
# Verificar no management UI: http://localhost:15672
# Confirmar no saldo da API
curl http://localhost:8080/contas/<guid>/saldo \
  -H "X-Api-Key: dev-key-local-somente"
```

---

## 9. O que ficou de fora (e por quê)

| Item | Razão |
|---|---|
| Publisher confirms (ACK do broker) | Adiciona latência e complexidade; para o escopo do desafio, `Persistent=true` é suficiente. Com mais tempo: implementar `WaitForConfirmsOrDie` ou confirms assíncronos. |
| Outbox Pattern no Produtor | Necessário para garantia at-least-once se o processo cair entre a requisição HTTP e o `BasicPublish`. Escopo futuro. |
| Múltiplos consumers (scale-out) | `prefetchCount: 1` + idempotência já garante que múltiplas instâncias do Worker operem corretamente. Basta subir mais réplicas. |
| Reprocessamento automático da DLQ | Requer UI ou endpoint de gestão. Para o desafio, inspeção via management UI é suficiente. |
| MassTransit | Optou-se por `RabbitMQ.Client` puro para expor explicitamente o comportamento de topologia, retry e DLQ — escolha mais didática para o contexto de desafio técnico. |
