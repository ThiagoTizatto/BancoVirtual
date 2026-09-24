# Architecture Decision Records (ADRs)

Registro das decisões arquiteturais do projeto, no formato Nygard/MADR:
**Contexto → Decisão → Alternativas consideradas → Consequências → Status**.

Os ADR-001..006 estão embutidos no design doc original
([2026-09-22-movimentacoes-financeiras-design.md](../superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md)).
A partir do ADR-007, cada decisão é um arquivo separado, referente ao
[hardening para produção](../superpowers/specs/2026-09-23-hardening-producao-design.md).

---

## Decisões críticas

Agrupamento por área para leitura rápida. ADRs marcados com ⭐ têm diagrama de fluxo no próprio arquivo.

### Concorrência e consistência

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-029](0029-update-atomico-saldo.md) ⭐ | Update atômico do saldo | Elimina race conditions em movimentações concorrentes sem lock pessimista; crédito nunca é perdido |
| [ADR-028](0028-idempotencia-sem-ttl.md) ⭐ | Idempotência por chave única no banco | Garante que nenhuma movimentação seja processada duas vezes, mesmo sob retry de cliente |

### Infraestrutura e persistência

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-009](0009-postgres-padrao-unico-testcontainers.md) | PostgreSQL como padrão único + Testcontainers | MVCC real nos testes; elimina a incoerência "alta demanda vs SQLite single-writer" |
| [ADR-011](0011-migrations-ef.md) | Migrations EF Core (vs `EnsureCreated`) | Schema versionado, evoluível sem perda de dados — pré-requisito para qualquer produção |
| [ADR-014](0014-saldo-atual-dinheiro.md) | `SaldoAtual` como `Dinheiro` VO | VO rico elimina aritmética `decimal` crua; `Reconstituir` separa rehidratação de criação |

### Performance

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-016](0016-saldo-historico-sql.md) ⭐ | Saldo histórico via agregação SQL | Trafega um escalar, não toda a série histórica de lançamentos — O(1) vs O(n) de rede |

### Resiliência

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-024](0024-circuit-breaker-policywrap.md) ⭐ | `PolicyWrap`: CircuitBreaker + Retry separados | Retry para contenção transitória; circuit breaker para conectividade — misturar os dois é antipadrão |

### Segurança

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-018](0018-api-key-vs-jwt.md) ⭐ | API Key (vs JWT) | Autenticação stateless proporcional ao escopo; JWT seria overhead sem benefício no contexto |
| [ADR-022](0022-secrets-em-camadas.md) | Secrets em camadas (user-secrets → env vars) | Nenhum segredo versionado; caminho de evolução para Vault sem refatorar a leitura |
| [ADR-023](0023-rate-limiting.md) | Rate limiting por API Key | Isola consumo entre clientes; 429 antes de saturar a aplicação sob pico |

### Observabilidade

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-025](0025-metricas-opentelemetry.md) | OpenTelemetry + Prometheus | Métricas vendor-neutral em `/metrics`; base para alertas e dashboards sob carga |
| [ADR-017](0017-problemdetails-nativo.md) | ProblemDetails nativo (RFC-7807) | Respostas de erro uniformes em todos os endpoints; elimina serialização manual |

### Qualidade e operação

| ADR | Decisão | Por que importa |
|-----|---------|-----------------|
| [ADR-008](0008-treat-warnings-as-errors.md) | `TreatWarningsAsErrors` | Build falha antes de merges sujos; CI aplica via `-warnaserror` |
| [ADR-015](0015-encapsular-infra-internal.md) | Infraestrutura `internal` + `InternalsVisibleTo` | Impede que camadas externas instanciem repositórios diretamente; DIP reforçado |
| [ADR-027](0027-ci-github-actions.md) | CI GitHub Actions | Build + testes automáticos; Testcontainers sobe Postgres efêmero no runner sem config extra |

---

| ADR | Título | Status |
|-----|--------|--------|
| [ADR-007](0007-inicializadores-de-camada.md) | Inicializadores de camada e composition root magra | Aceito |
| [ADR-008](0008-treat-warnings-as-errors.md) | TreatWarningsAsErrors / build sob trava | Aceito |
| [ADR-009](0009-postgres-padrao-unico-testcontainers.md) | PostgreSQL como padrão único + Testcontainers | Aceito |
| [ADR-010](0010-versao-linha-guid-manual.md) | VersaoLinha como Guid manual (vs xmin) | Aceito |
| [ADR-011](0011-migrations-ef.md) | Migrations EF (vs EnsureCreated) | Aceito |
| [ADR-012](0012-docker-compose.md) | Docker/docker-compose como caminho principal | Aceito |
| [ADR-013](0013-lancamento-valor-decimal.md) | Lancamento.Valor permanece decimal | Aceito |
| [ADR-014](0014-saldo-atual-dinheiro.md) | SaldoAtual como Dinheiro + Reconstituir | Aceito |
| [ADR-015](0015-encapsular-infra-internal.md) | Encapsular infra com internal + InternalsVisibleTo | Aceito |
| [ADR-016](0016-saldo-historico-sql.md) | Saldo histórico via agregação SQL | Aceito |
| [ADR-017](0017-problemdetails-nativo.md) | ProblemDetails nativo | Aceito |
| [ADR-018](0018-api-key-vs-jwt.md) | API Key (vs JWT) | Aceito |
| [ADR-019](0019-https-hsts.md) | HTTPS + HSTS | Aceito |
| [ADR-020](0020-redaction-serilog.md) | Redaction como policy de código no Serilog | Aceito |
| [ADR-021](0021-api-keys-configuracao.md) | API Keys em configuração (vs tabela) | Aceito |
| [ADR-022](0022-secrets-em-camadas.md) | Estratégia de secrets em camadas | Aceito |
| [ADR-023](0023-rate-limiting.md) | Rate limiting particionado por API Key | Aceito |
| [ADR-024](0024-circuit-breaker-policywrap.md) | Circuit breaker separado do retry (PolicyWrap) | Aceito |
| [ADR-025](0025-metricas-opentelemetry.md) | OpenTelemetry + Prometheus para métricas | Aceito |
| [ADR-026](0026-load-test-nbomber.md) | NBomber para load test, fora do CI | Aceito |
| [ADR-027](0027-ci-github-actions.md) | CI GitHub Actions com -warnaserror | Aceito |
| [ADR-028](0028-idempotencia-sem-ttl.md) | Idempotência sem TTL (com trilha de evolução) | Aceito |
| [ADR-029](0029-update-atomico-saldo.md) | Update atômico do saldo (elimina contenção sob concorrência) | Aceito |
