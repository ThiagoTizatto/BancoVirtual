# ADR-023 — Rate limiting particionado por API Key

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O desafio pede tratar *"alta demanda"*. Sem controle de taxa, um cliente pode
saturar a aplicação e degradar o serviço para os demais — problema exatamente do
tipo que o enunciado descreve ("instabilidade em horários de pico").

## Decisão

Usar o `AddRateLimiter` nativo do .NET, registrado em `AddApi()`, particionado por
API Key (fallback por IP quando ausente). Requisições acima do limite recebem `429`
em formato ProblemDetails (ADR-017). `app.UseRateLimiter()` no pipeline.

## Alternativas consideradas

- **Sem rate limiting** — deixa a aplicação exposta a saturação por cliente.
- **Rate limiting no gateway/ingress** — comum em produção, mas deixa a aplicação
  sem proteção standalone; defesa em profundidade é preferível e o middleware nativo
  tem custo baixo.
- **Biblioteca externa (ex: AspNetCoreRateLimit)** — o limiter nativo do .NET 7+
  cobre o caso sem dependência extra.

## Consequências

- Proteção por cliente contra picos, alinhada ao tema de alta demanda.
- Particionamento por API Key isola o consumo entre clientes.
- Um contador de rejeições alimenta as métricas (ADR-025).
- Custo: calibrar limites (janela/quota) conforme a capacidade real.
