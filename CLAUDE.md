# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Comandos

```bash
dotnet build
dotnet test                                              # suíte completa (domínio + API)

# Rodar a API localmente (requer PostgreSQL — suba via docker compose up postgres -d)
dotnet run --project src/MovimentacoesFinanceiras.Api   # sobe em http://localhost:5095 (Swagger na raiz)

# Via Docker (recomendado — sobe API + PostgreSQL juntos)
docker compose up --build

# Rodar um projeto de teste isolado
dotnet test tests/Dominio.Testes
dotnet test tests/Api.Testes

# Rodar um único teste por nome
dotnet test --filter "FullyQualifiedName~ContaTestes"
dotnet test --filter "DisplayName~Debitar"
```

O banco é **PostgreSQL**. O schema é gerenciado por **migrations EF Core** aplicadas automaticamente no startup via `MigrateAsync` (exceto no ambiente `"Testing"`). Para desenvolvimento local, suba o banco com `docker compose up postgres -d` e rode a API normalmente. Nos testes de integração, o banco é provisionado via **Testcontainers** — um contêiner PostgreSQL efêmero por suíte.

Target framework: **.NET 10**.

## Arquitetura

Clean Architecture + DDD + CQRS. Dependências apontam sempre para dentro: `Api → Aplicacao → Dominio`, com `Infraestrutura` implementando as interfaces do domínio. Um único Bounded Context (`Contas`), com o agregado `Conta` como raiz.

- **Dominio** — `Conta` (raiz de agregado, construtor privado + factory `Criar`), `Lancamento` (entidade), `Dinheiro` (VO), `TipoLancamento` (enum). Regras de negócio vivem aqui: `Conta.Debitar` lança `SaldoInsuficienteException`, nunca deixa saldo negativo. Sem dependências externas.
- **Aplicacao** — CQRS via MediatR. Commands em `Contas/Commands/`, Queries em `Contas/Queries/`. `ValidationBehavior` (pipeline MediatR) roda os validators FluentValidation antes de cada handler. `PoliticasResiliencia` encapsula retry + circuit breaker (Polly). `MetricasMovimentacao` expõe contador via `Meter` nativo.
- **Infraestrutura** — EF Core + PostgreSQL (Npgsql). `BancoDadosContext`, `ContaRepository` (implementa `IContaRepository` do domínio), configurations aplicadas via `ApplyConfigurationsFromAssembly`.
- **Api** — Controllers, middlewares, Serilog (JSON no console, redação de campos sensíveis via `IDestructuringPolicy`), Swagger, autenticação API Key (`ApiKeyAuthenticationHandler`), rate limiter (100 req/10 s por chave), OpenTelemetry + Prometheus em `/metrics`.

### Padrões centrais (leia antes de mexer em escrita/saldo)

- **Ledger append-only**: cada crédito/débito é um `Lancamento` imutável inserido na tabela. `Conta.SaldoAtual` é um **snapshot** mantido em paralelo ao ledger — consulta de saldo atual é O(1); saldo histórico (`?em=<data>`) soma os lançamentos até a data via `ConsultarSaldoEmAsync`. As duas fontes precisam permanecer consistentes: qualquer nova operação de movimentação atualiza `SaldoAtual` **e** adiciona um `Lancamento`.

- **Concorrência**: o saldo é atualizado via `UPDATE ... SET saldo_atual = saldo_atual ± valor` (SQL atômico), eliminando race conditions sem otimistic locking. `RegistrarMovimentacaoHandler` envolve a chamada ao repositório em `PoliticasResiliencia.Combinada` — `PolicyWrap` que combina retry de `DbUpdateConcurrencyException` (3 tentativas, backoff exponencial) com circuit breaker de conectividade (`NpgsqlException` / `DbUpdateException`) que abre após 5 falhas consecutivas e permanece aberto 15 s.

- **Idempotência**: header HTTP `Idempotency-Key` → `Lancamento.ChaveIdempotencia`, com índice único no banco. O handler verifica a chave **antes** de qualquer escrita e retorna o lançamento existente se já processado.

- **Autenticação**: `ApiKeyAuthenticationHandler` valida o header `X-Api-Key` contra a lista `ApiKeys` (configurada via user-secrets ou env vars). Todos os endpoints em `ContasController` requerem `[Authorize]`. `/health` e `/metrics` são anônimos. A lambda em `AddScheme` é lazy — lê `ApiKeys` somente na primeira requisição, após `Build()`, garantindo que `WebApplicationFactory.ConfigureAppConfiguration` já tenha injetado as chaves de teste.

- **Rate limiting**: `[EnableRateLimiting("por-api-key")]` no controller. Partição por header `X-Api-Key` (fallback: IP). 100 req/10 s, sem fila (`QueueLimit = 0`). Rejeição retorna 429.

- **Observabilidade**: `MetricasMovimentacao` usa `IMeterFactory` para criar o `Meter` `"MovimentacoesFinanceiras"` e o contador `movimentacoes.total` (tag `tipo`). OpenTelemetry exporta via Prometheus em `/metrics`; instrumentação AspNetCore expõe `http.server.request.duration` automaticamente.

- **Tratamento de erros**: exceções de domínio NÃO viram `if`/status code no controller. O `TratadorDeExcecoesMiddleware` mapeia cada exceção para RFC-7807 (`application/problem+json`): `ValidationException`→400, `SaldoInsuficienteException`→422, `ContaNaoEncontradaException`→404. Nova exceção de domínio → adicione um `catch` lá.

### Testes

- `Dominio.Testes` — unitários puros do agregado e VOs.
- `Api.Testes` — integração via `WebApplicationFactory<Program>` (`AplicacaoFactory`), ambiente `"Testing"` (que desativa o `MigrateAsync` do `Program.cs` e usa PostgreSQL via Testcontainers). Inclui testes de concorrência que disparam movimentações em paralelo, e um caso 401 sem API Key. `Program.cs` expõe `public partial class Program {}` justamente para essa factory. `AplicacaoFactory.CriarClienteAutenticado()` injeta a API Key de teste automaticamente.
- `Carga.Testes` — projeto console NBomber para load test sob demanda (não roda no CI). Execução: `dotnet run --project tests/Carga.Testes -c Release`.

O CI (`.github/workflows/ci.yml`) roda `dotnet build -warnaserror` + `dotnet test` em `ubuntu-latest`. Testcontainers sobe o PostgreSQL efêmero no runner via Docker (disponível no runner padrão).

## Convenções (deste repositório)

- Linguagem ubíqua do domínio em **português** (`Conta`, `Lancamento`, `Dinheiro`, `Saldo`, `Creditar`, `Debitar`); termos técnicos consagrados em **inglês** (`Command`, `Query`, `Handler`, `Repository`, `Behavior`, `Request`/`Response`).
- Testes seguem **AAA** com blocos separados por comentários `// Arrange`/`// Act`/`// Assert`.
- **Nunca use `else`/`else if`** — early returns, guard clauses, pattern matching, expressões.
