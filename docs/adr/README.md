# Architecture Decision Records (ADRs)

Registro das decisões arquiteturais do projeto, no formato Nygard/MADR:
**Contexto → Decisão → Alternativas consideradas → Consequências → Status**.

Os ADR-001..006 estão embutidos no design doc original
([2026-09-22-movimentacoes-financeiras-design.md](../superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md)).
A partir do ADR-007, cada decisão é um arquivo separado, referente ao
[hardening para produção](../superpowers/specs/2026-09-23-hardening-producao-design.md).

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
