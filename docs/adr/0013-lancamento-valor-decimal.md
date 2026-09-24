# ADR-013 — Lancamento.Valor permanece decimal

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-014

## Contexto

Ao enriquecer `Dinheiro` como Value Object (ADR-014) e transformar
`Conta.SaldoAtual` em `Dinheiro`, surge a questão de fazer o mesmo com
`Lancamento.Valor`, hoje `decimal`.

## Decisão

`Lancamento.Valor` permanece `decimal`. O `Dinheiro` é usado como VO na entrada
das operações (`Creditar`/`Debitar` recebem `Dinheiro`) e no `SaldoAtual`, mas o
valor persistido no ledger continua `decimal`.

## Alternativas consideradas

- **`Lancamento.Valor` também vira `Dinheiro`** — consistência total do VO no
  domínio, mas adiciona value converter próprio e complica as queries agregadas
  (`SUM` sobre a coluna, ver ADR-016) sem ganho de invariante relevante: o valor
  do lançamento é imutável e sempre positivo por construção nas factories
  `CriarCredito`/`CriarDebito`.

## Consequências

- Escopo enxuto: a mudança de VO concentra-se onde o DDD mais cobra (o saldo).
- Queries de `SUM` no banco permanecem simples (coluna `decimal` direta).
- O ledger é um registro imutável — o valor já nasce validado (`Dinheiro.De`
  exige `> 0`) antes de virar `Lancamento`, então a proteção de invariante não
  se perde.
