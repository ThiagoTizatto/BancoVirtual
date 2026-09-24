# ADR-017 — ProblemDetails nativo

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O `TratadorDeExcecoesMiddleware` montava a resposta de erro serializando JSON
manualmente via `JsonSerializer`, inspirado em RFC-7807 mas sem o schema completo
nem a integração com o pipeline do ASP.NET Core. Reinventava um mecanismo que o
framework já oferece.

## Decisão

Migrar para `IProblemDetailsService` + `AddProblemDetails()` do ASP.NET Core,
preservando os mapeamentos de exceção de domínio:

- `ValidationException` → 400
- `SaldoInsuficienteException` → 422
- `ContaNaoEncontradaException` → 404
- catch-all → 500

Com schema completo (`type`, `title`, `status`, `detail`, `instance`) e extensions
para `erros` e `correlacao_id`. `Content-Type: application/problem+json`.

## Alternativas consideradas

- **Manter serialização manual** — funciona, mas mais código para manter, sem
  conformidade total com o schema e sem integração com o tratamento nativo.

## Consequências

- Menos código próprio; conformidade completa com RFC-7807.
- Integra com respostas de erro do framework (401, 429) de forma uniforme.
- A regra de nunca vazar detalhes internos no 500 é mantida.
- Custo: revisar a forma dos testes que assertavam o corpo do erro.
