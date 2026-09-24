# ADR-010 — VersaoLinha como Guid manual (vs xmin nativo)

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

Com a migração para PostgreSQL (ADR-009), surge a opção de usar a coluna de sistema
`xmin` do Postgres como token de concorrência otimista — abordagem idiomática do
Npgsql (`UseXminAsConcurrencyToken()`). Hoje a concorrência usa `VersaoLinha`, um
`Guid` regenerado a cada `Creditar`/`Debitar` e marcado `IsConcurrencyToken()`.

## Decisão

Manter `VersaoLinha` como `Guid` manual, controlado pelo domínio.

## Alternativas consideradas

- **`xmin` nativo do Postgres** — zero código de gestão de versão, mantido pelo
  próprio banco. Porém acopla o modelo de concorrência a um detalhe do provider:
  o domínio passaria a depender de uma coluna de sistema específica do Postgres,
  reduzindo portabilidade e testabilidade fora do banco real.

## Consequências

- O domínio permanece agnóstico ao provider — `VersaoLinha` é um conceito de
  domínio, testável em unit tests puros sem banco.
- Custo: uma coluna `uuid` explícita e a regeneração manual em cada operação de
  escrita (já existente).
- Se no futuro a portabilidade deixar de importar, migrar para `xmin` é uma
  mudança contida na Infraestrutura.
