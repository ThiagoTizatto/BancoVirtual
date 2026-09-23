# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Comandos

```bash
dotnet build
dotnet test                                              # suíte completa (domínio + API)
dotnet run --project src/MovimentacoesFinanceiras.Api   # sobe em http://localhost:5095 (Swagger na raiz)

# Rodar um projeto de teste isolado
dotnet test tests/Dominio.Testes
dotnet test tests/Api.Testes

# Rodar um único teste por nome
dotnet test --filter "FullyQualifiedName~ContaTestes"
dotnet test --filter "DisplayName~Debitar"
```

O banco SQLite (`movimentacoes.db`) é criado automaticamente via `EnsureCreatedAsync` no startup — **não há migrations**. Alterações no modelo EF são refletidas apagando o `.db` e reiniciando (ou recriando o banco de teste, que usa arquivo temporário por instância de `AplicacaoFactory`).

Target framework: **.NET 10**.

## Arquitetura

Clean Architecture + DDD + CQRS. Dependências apontam sempre para dentro: `Api → Aplicacao → Dominio`, com `Infraestrutura` implementando as interfaces do domínio. Um único Bounded Context (`Contas`), com o agregado `Conta` como raiz.

- **Dominio** — `Conta` (raiz de agregado, construtor privado + factory `Criar`), `Lancamento` (entidade), `Dinheiro` (VO), `TipoLancamento` (enum). Regras de negócio vivem aqui: `Conta.Debitar` lança `SaldoInsuficienteException`, nunca deixa saldo negativo. Sem dependências externas.
- **Aplicacao** — CQRS via MediatR. Commands em `Contas/Commands/`, Queries em `Contas/Queries/`. `ValidationBehavior` (pipeline MediatR) roda os validators FluentValidation antes de cada handler.
- **Infraestrutura** — EF Core + SQLite. `BancoDadosContext`, `ContaRepository` (implementa `IContaRepository` do domínio), configurations aplicadas via `ApplyConfigurationsFromAssembly`.
- **Api** — Controllers, middlewares, Serilog (JSON no console), Swagger.

### Padrões centrais (leia antes de mexer em escrita/saldo)

- **Ledger append-only**: cada crédito/débito é um `Lancamento` imutável inserido na tabela. `Conta.SaldoAtual` é um **snapshot** mantido em paralelo ao ledger — consulta de saldo atual é O(1); saldo histórico (`?em=<data>`) soma os lançamentos até a data via `ConsultarSaldoEmAsync`. As duas fontes precisam permanecer consistentes: qualquer nova operação de movimentação atualiza `SaldoAtual` **e** adiciona um `Lancamento`.

- **Concorrência otimista**: `Conta.VersaoLinha` é um `Guid` marcado `IsConcurrencyToken()` e regenerado a cada `Creditar`/`Debitar`. `RegistrarMovimentacaoHandler` envolve a escrita numa **política Polly** (3 retries com backoff exponencial) que captura `DbUpdateConcurrencyException`. **Cada tentativa cria um novo escopo de DI** (`IServiceScopeFactory` → novo `DbContext`), porque um `DbContext` fica poluído após uma exceção de concorrência e não pode ser reusado.

- **Idempotência**: header HTTP `Idempotency-Key` → `Lancamento.ChaveIdempotencia`, com índice único no banco. O handler verifica a chave **antes** de qualquer escrita e retorna o lançamento existente se já processado. SQLite trata múltiplos NULLs como distintos, então o índice único não bloqueia lançamentos sem chave.

- **Tracking de `Lancamento`**: o handler chama `AdicionarLancamentoAsync` explicitamente em vez de confiar no tracking pela coleção de navegação — o backing field `_lancamentos` não é rastreado corretamente quando a `Conta` é carregada sem `Include`.

- **Tratamento de erros**: exceções de domínio NÃO viram `if`/status code no controller. O `TratadorDeExcecoesMiddleware` mapeia cada exceção para RFC-7807 (`application/problem+json`): `ValidationException`→400, `SaldoInsuficienteException`→422, `ContaNaoEncontradaException`→404. Nova exceção de domínio → adicione um `catch` lá.

### Testes

- `Dominio.Testes` — unitários puros do agregado e VOs.
- `Api.Testes` — integração via `WebApplicationFactory<Program>` (`AplicacaoFactory`), ambiente `"Testing"` (que desativa o `EnsureCreatedAsync` do `Program.cs` e usa SQLite em arquivo temporário). Inclui testes de concorrência que disparam movimentações em paralelo. `Program.cs` expõe `public partial class Program {}` justamente para essa factory.

## Convenções (deste repositório)

- Linguagem ubíqua do domínio em **português** (`Conta`, `Lancamento`, `Dinheiro`, `Saldo`, `Creditar`, `Debitar`); termos técnicos consagrados em **inglês** (`Command`, `Query`, `Handler`, `Repository`, `Behavior`, `Request`/`Response`).
- Testes seguem **AAA** com blocos separados por comentários `// Arrange`/`// Act`/`// Assert`.
- **Nunca use `else`/`else if`** — early returns, guard clauses, pattern matching, expressões.
