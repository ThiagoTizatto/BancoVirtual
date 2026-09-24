# ADR-025 — OpenTelemetry + Prometheus para métricas

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O tema central do desafio é *performance e estabilidade sob pico*. Sem métricas, é
impossível sustentar afirmações sobre comportamento sob carga — não há visibilidade
de latência, throughput ou frequência de conflitos. Havia apenas logs estruturados
e correlation ID.

## Decisão

Instrumentar com `System.Diagnostics.Metrics.Meter` (API nativa do .NET):

- Contador de movimentações por tipo (crédito/débito).
- Histograma de latência de handler.
- Contador de conflitos de concorrência (retries do Polly).
- Contador de rejeições de rate limit.

Expor via OpenTelemetry (`AddOpenTelemetry().WithMetrics(...)`) com exporter
Prometheus no endpoint `/metrics`.

## Alternativas consideradas

- **Sem métricas** — não sustenta o discurso de comportamento sob carga.
- **Endpoint de métricas caseiro** — evita dependência, mas reinventa formato e
  perde a interoperabilidade com o ecossistema de observabilidade.
- **Application Insights / vendor específico** — acopla a um provedor; OpenTelemetry
  é vendor-neutral e o padrão de indústria.

## Consequências

- Métricas em formato padrão, consumíveis por Prometheus/Grafana.
- Instrumentos nativos, sobrecarga baixa.
- Base para dashboards e alertas (evolução).
- Custo: dependências OpenTelemetry + endpoint `/metrics` (protegido/segmentado
  conforme necessidade de deploy).
