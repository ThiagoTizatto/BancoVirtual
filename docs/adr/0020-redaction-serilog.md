# ADR-020 — Redaction como policy de código no Serilog

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-006

## Contexto

O design original afirmava que campos sensíveis (`valor`, `saldo_atual`, `descricao`)
não apareciam nos logs. Era uma *afirmação*, não uma *garantia*: nada no código
impedia que uma futura instrução de log vazasse esses dados.

## Decisão

Transformar a afirmação em garantia de código: uma `IDestructuringPolicy` custom no
Serilog que redige campos sensíveis (`valor`, `saldo`, `descricao` e PII) ao
serializar objetos. Revisar os log statements existentes. Configurar
`UseSerilogRequestLogging` para não capturar corpo de request/response.

## Alternativas consideradas

- **Confiar em disciplina de código** — frágil; um log novo pode vazar sem que nada
  falhe.
- **Mascarar no coletor/agregador** (ex: regra no Elastic) — protege no destino, mas
  o dado sensível ainda deixa a aplicação; redigir na origem é mais seguro.

## Consequências

- Campos sensíveis nunca são serializados nos logs, por construção.
- Defesa na origem, independente do coletor de logs.
- Custo: manter a lista de campos sensíveis atualizada conforme o modelo evolui.
- `correlacao_id` (ADR-006) permanece para rastreabilidade sem expor dados.
