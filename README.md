# Sistema de Movimentações Financeiras

Sistema bancário para registro de movimentações financeiras (créditos e débitos) e consulta de saldo — atual e histórico (_point-in-time_) — construído como solução para o desafio técnico de arquiteto de software.

## Como rodar

### Via Docker (recomendado)

```bash
docker compose up --build
```

A API sobe em `http://localhost:8080` (Swagger na raiz) e o PostgreSQL sobe automaticamente. As migrations são aplicadas no startup.

### Local (requer PostgreSQL)

Suba apenas o banco e rode a API localmente:

```bash
docker compose up postgres -d
dotnet run --project src/MovimentacoesFinanceiras.Api
```

### Executar os testes

```bash
dotnet test
```

Suíte atual: **37 testes** (23 de domínio + 14 de API, cobrindo integração, concorrência e autenticação).

---

## Configuração e segredos

Todas as chamadas à API (exceto `/saude`) requerem o header `X-Api-Key`.

### Desenvolvimento local

As chaves de API e a connection string são gerenciadas via **user-secrets** (nunca em `appsettings.json`):

```bash
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres" \
  --project src/MovimentacoesFinanceiras.Api

dotnet user-secrets set "ApiKeys:0" "dev-key-local-somente" \
  --project src/MovimentacoesFinanceiras.Api
```

O `appsettings.Development.json` já traz uma chave de conveniência para uso local (`dev-key-local-somente`).

### Docker / produção

Forneça via variáveis de ambiente:

```
ConnectionStrings__Postgres=Host=db;Port=5432;Database=movimentacoes;Username=postgres;Password=<senha>
ApiKeys__0=<chave-de-producao>
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

### Registrar um crédito

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
curl http://localhost:8080/saude
```

### Rate limiting

Cada API Key tem uma janela fixa de **100 requisições / 10 segundos**. Excedido o limite, a API retorna `429 Too Many Requests`.

### Observabilidade

| Endpoint | Descrição |
|---|---|
| `/metrics` | Métricas no formato Prometheus (`movimentacoes_total`, `http.server.request.duration`) |
| `/saude` | Health check do banco de dados (sem autenticação) |

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

A solução usa **CQRS + Ledger append-only** dentro de uma **Clean Architecture** com **DDD**. A documentação completa das decisões arquiteturais está em:

- [`docs/superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md`](docs/superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md) — Design doc completo com ADRs

### Camadas

```
Dominio         → agregado Conta, Lancamento, Dinheiro (VO), regras de negócio puras
Aplicacao       → CQRS via MediatR, validação (FluentValidation), retry (Polly)
Infraestrutura  → EF Core + PostgreSQL, ContaRepositorio
Api             → ASP.NET Core, controladores, middlewares, Serilog, Swagger, autenticação API Key, rate limiting, OpenTelemetry/Prometheus
```

O fluxo de dependências aponta sempre para dentro (`Api → Aplicacao → Dominio`); o domínio não conhece nenhuma dependência externa.

### Decisões principais

| Decisão | Justificativa |
|---|---|
| Ledger append-only | Rastreabilidade imutável; saldo histórico via soma dos lançamentos sem mecanismo extra |
| CQRS | Leitura e escrita com modelos e caminhos independentes |
| UPDATE atômico + Polly PolicyWrap | UPDATE SQL atômico evita race conditions sem bloqueio; Polly combina retry de concorrência com circuit breaker de conectividade |
| Idempotência via `Idempotency-Key` | Retries do cliente nunca geram lançamentos duplicados |
| Snapshot `saldo_atual` + ledger | Consulta de saldo atual em O(1); ledger garante consistência e auditoria |
| PostgreSQL | Banco relacional robusto com transações ACID e índices únicos |
| API Key + rate limiting | Autenticação simples e proteção contra abuso (100 req/10 s por chave) |
| OpenTelemetry/Prometheus | Métricas instrumentadas em `/metrics`; pronto para Grafana/alertas |

### O que ficou de fora (e por quê)

- **Autenticação JWT / OAuth2** — API Key suficiente para o escopo do desafio; upgrade documentado como ponto de extensão
- **Snapshot periódico de saldo histórico** — necessário para históricos de anos com alto volume; para o escopo, a soma do ledger resolve
