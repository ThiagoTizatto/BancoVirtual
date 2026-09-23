# ADR-026 — NBomber para load test, fora do CI

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O problema é literalmente sobre performance sob pico. Sem um teste de carga,
qualquer afirmação sobre capacidade ("aguenta o pico") é opinião, não evidência.

## Decisão

Criar um cenário de load test com **NBomber** (`tests/Carga.Testes` ou script
standalone): movimentações concorrentes contra a API, medindo throughput e latência
p95/p99. **Não roda no CI** por padrão (é lento e caro); é executável sob demanda,
com resultados-exemplo documentados no README.

## Alternativas consideradas

- **k6** — excelente ferramenta, mas em JS/Go, fora do stack .NET; NBomber mantém
  tudo em C# e integra ao repositório.
- **Rodar no CI** — daria regressão contínua de performance, mas encareceria e
  alentaria o pipeline; desproporcional ao escopo.
- **Sem load test** — deixa o eixo de performance sem evidência.

## Consequências

- "Aguenta o pico" vira número (throughput, p95/p99), não opinião.
- Mesmo stack (C#), sem ferramenta externa de linguagem diferente.
- Execução manual mantém o CI rápido.
- Custo: manter o cenário atualizado conforme a API evolui.
