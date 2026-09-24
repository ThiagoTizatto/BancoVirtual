# Sistema de Movimentações Financeiras

Sistema bancário para registro de movimentações financeiras (créditos e débitos) e consulta de saldo — atual e histórico (_point-in-time_) — construído como solução para o desafio técnico de arquiteto de software.

A solução evolui o registro síncrono original para um modelo assíncrono com **RabbitMQ**: o **Produtor** publica comandos de movimentação em uma fila; a **Api** processa a fila via `BackgroundService` interno e persiste os lançamentos no PostgreSQL.

## Como rodar

### Via Docker (recomendado)

```bash
docker compose up --build
```

Sobe quatro serviços:

| Serviço | URL | Descrição |
|---|---|---|
| `api` | `http://localhost:8080` | API REST + consumidor RabbitMQ interno |
| `produtor` | `http://localhost:8081` | Endpoint de publicação de movimentações |
| `rabbitmq` | `http://localhost:15672` | Management UI (guest/guest) |
| `postgres` | `localhost:5432` | PostgreSQL |

As migrations são aplicadas no startup da Api. A topologia RabbitMQ (exchanges, filas, DLQ) é declarada antes de iniciar o consumidor.

### Local (requer PostgreSQL + RabbitMQ)

```bash
docker compose up postgres rabbitmq -d
dotnet run --project src/MovimentacoesFinanceiras.Api
# Em outro terminal (opcional):
dotnet run --project src/MovimentacoesFinanceiras.Produtor
```

### Executar os testes

```bash
dotnet test
```

Suíte atual: **42 testes** — 23 de domínio, 14 de API (integração via Testcontainers, concorrência, autenticação), 3 do consumidor RabbitMQ e 2 do publicador.

---

## Configuração e segredos

Todas as chamadas à API (exceto `/health`) requerem o header `X-Api-Key`.

### Desenvolvimento local

As chaves de API e a connection string são gerenciadas via **user-secrets** (nunca em `appsettings.json`):

```bash
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres" \
  --project src/MovimentacoesFinanceiras.Api

dotnet user-secrets set "ApiKeys:0" "dev-key-local-somente" \
  --project src/MovimentacoesFinanceiras.Api
```

O `appsettings.Development.json` já traz uma chave de conveniência para uso local (`dev-key-local-somente`).

O RabbitMQ é configurado pela seção `RabbitMq` (valores padrão apontam para `localhost:5672` com `guest/guest`).

### Docker / produção

Forneça via variáveis de ambiente:

```
ConnectionStrings__Postgres=Host=db;Port=5432;Database=movimentacoes;Username=postgres;Password=<senha>
ApiKeys__0=<chave-de-producao>
RabbitMq__Host=rabbitmq
RabbitMq__Username=<usuario>
RabbitMq__Password=<senha>
```

---

## Exemplos de uso

### Criar uma conta

```bash
curl -X POST http://localhost:8080/contas \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-key-local-somente" \
  -d '{"clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
```

### Publicar uma movimentação via Produtor (fluxo assíncrono)

O Produtor publica a movimentação na fila RabbitMQ e retorna `202 Accepted`. A Api processa e persiste de forma assíncrona.

```bash
curl -X POST http://localhost:8081/movimentacoes \
  -H "Content-Type: application/json" \
  -d '{
    "contaId": "{id}",
    "tipo": "Credito",
    "valor": 100.00,
    "descricao": "Depósito inicial",
    "chaveIdempotencia": "8f3a1c2e-4a5b-6c7d-8e9f-0a1b2c3d4e5f"
  }'
```

### Registrar uma movimentação diretamente na API (fluxo síncrono)

```bash
curl -X POST http://localhost:8080/contas/{id}/movimentacoes \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-key-local-somente" \
  -d '{"tipo": "Credito", "valor": 100.00, "descricao": "Depósito inicial"}'
```

### Registrar um débito idempotente

O cabeçalho `Idempotency-Key` garante que retries do cliente não gerem lançamentos duplicados.

```bash
curl -X POST http://localhost:8080/contas/{id}/movimentacoes \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-key-local-somente" \
  -H "Idempotency-Key: 8f3a1c2e-..." \
  -d '{"tipo": "Debito", "valor": 30.00, "descricao": "Saque"}'
```

### Consultar saldo atual

```bash
curl http://localhost:8080/contas/{id}/saldo \
  -H "X-Api-Key: dev-key-local-somente"
```

### Consultar saldo em ponto no tempo

```bash
curl "http://localhost:8080/contas/{id}/saldo?em=2025-01-15T12:00:00Z" \
  -H "X-Api-Key: dev-key-local-somente"
```

### Listar extrato paginado

```bash
curl "http://localhost:8080/contas/{id}/movimentacoes?pagina=1&tamanhoPagina=20" \
  -H "X-Api-Key: dev-key-local-somente"
```

### Health check (sem autenticação)

```bash
curl http://localhost:8080/health
```

### Rate limiting

Cada API Key tem uma janela fixa de **100 requisições / 10 segundos**. Excedido o limite, a API retorna `429 Too Many Requests`.

### Observabilidade

| Endpoint | Descrição |
|---|---|
| `/metrics` | Métricas no formato Prometheus (`movimentacoes_total`, `http.server.request.duration`) |
| `/health` | Health check do banco de dados (sem autenticação) |

Exemplo de scraping:
```bash
curl http://localhost:8080/metrics
```

### Load test (execução sob demanda)

Com a API no ar (`docker compose up`):

```bash
CARGA_BASE_URL=http://localhost:8080 \
CARGA_API_KEY=dev-key-local-somente \
dotnet run --project tests/Carga.Testes -c Release
```

NBomber injeta 100 requisições/s por 30 s e gera relatório com throughput e latências p95/p99.

---

## Arquitetura

A solução usa **CQRS + Ledger append-only** dentro de uma **Clean Architecture** com **DDD**, evoluída para um modelo assíncrono com **RabbitMQ**. A documentação completa das decisões arquiteturais está em:

- [`docs/superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md`](docs/superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md) — Design doc completo com ADRs

### Fluxo assíncrono

```
Cliente → POST /movimentacoes (Produtor :8081)
           └── publica ComandoMovimentacaoMessage → RabbitMQ exchange "movimentacoes"
                                                      └── fila "movimentacoes.registrar"
                                                            └── ConsumidorDeMovimentacoes (BackgroundService na Api)
                                                                  └── ProcessadorDeMovimentacao → MediatR → PostgreSQL
                                                            └── DLQ "movimentacoes.registrar.dlq" (falhas não-recuperáveis)
```

### Camadas

```
Dominio         → Conta (aggregate root), Lancamento, Dinheiro (VO), regras de negócio puras
Aplicacao       → CQRS via MediatR, validação (FluentValidation), retry (Polly)
Infraestrutura  → EF Core + PostgreSQL (Persistencia/)
                  RabbitMQ: conexão, topologia, opções (RabbitMq/)
                  Contas/RabbitMq/: ProcessadorDeMovimentacao, ConsumidorDeMovimentacoes
Mensagens       → contratos de mensagem compartilhados (ComandoMovimentacaoMessage, TopologiaRabbitMq)
Api             → ASP.NET Core, controladores, middlewares, Serilog, Swagger, autenticação API Key,
                  rate limiting, OpenTelemetry/Prometheus; hospeda o ConsumidorDeMovimentacoes
Produtor        → minimal API independente; publica mensagens na fila via PublicadorRabbitMq
```

O fluxo de dependências aponta sempre para dentro (`Api → Aplicacao → Dominio`); `Infraestrutura` referencia `Aplicacao` para despachar comandos MediatR (padrão adapter, sem ciclo).

### Decisões principais

| Decisão | Justificativa |
|---|---|
| Ledger append-only | Rastreabilidade imutável; saldo histórico via soma dos lançamentos sem mecanismo extra |
| CQRS | Leitura e escrita com modelos e caminhos independentes |
| UPDATE atômico + Polly PolicyWrap | UPDATE SQL atômico evita race conditions sem bloqueio; Polly combina retry de concorrência com circuit breaker de conectividade |
| Idempotência via `Idempotency-Key` / `MessageId` | Retries do cliente nunca geram lançamentos duplicados — tanto no fluxo síncrono (header HTTP) quanto no assíncrono (RabbitMQ MessageId) |
| Snapshot `saldo_atual` + ledger | Consulta de saldo atual em O(1); ledger garante consistência e auditoria |
| Consumer no mesmo processo da Api | Elimina complexidade operacional de um processo separado; BackgroundService registrado condicionalmente (não inicia em ambiente de testes) |
| Dead Letter Queue (DLQ) | Mensagens não processáveis são movidas para `movimentacoes.registrar.dlq` sem bloquear a fila principal |
| `prefetchCount: 1` por canal | Garante que cada mensagem seja totalmente processada antes da próxima ser entregue ao mesmo consumidor |
| PostgreSQL | Banco relacional robusto com transações ACID e índices únicos |
| API Key + rate limiting | Autenticação simples e proteção contra abuso (100 req/10 s por chave) |
| OpenTelemetry/Prometheus | Métricas instrumentadas em `/metrics`; pronto para Grafana/alertas |

### O que ficou de fora (e por quê)

- **Autenticação JWT / OAuth2** — API Key suficiente para o escopo do desafio; upgrade documentado como ponto de extensão
- **Snapshot periódico de saldo histórico** — necessário para históricos de anos com alto volume; para o escopo, a soma do ledger resolve
- **Múltiplos consumidores / particionamento** — para escalar o consumo, basta aumentar réplicas da Api (cada instância cria seu próprio canal com `prefetchCount: 1`)
