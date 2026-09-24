# ADR-018 — API Key (vs JWT)

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-021

## Fluxo do pipeline de autenticação e controle de acesso

```mermaid
flowchart TD
    REQ[HTTP Request] --> ANON{"endpoint\nanônimo?"}

    ANON -->|"/health, /metrics"| DIRECT[Responde diretamente\nsem autenticação]

    ANON -->|"demais endpoints\n[Authorize]"| AUTH[ApiKeyAuthenticationHandler\nlê header X-Api-Key]

    AUTH --> KEY{"chave presente\ne válida?"}
    KEY -->|ausente ou inválida| U401["401 Unauthorized\napplication/problem+json"]

    KEY -->|válida| RL["Rate Limiter — ADR-023\nparticionado por API Key\n100 req / 10 s"]
    RL --> QUOTA{"dentro da\ncota?"}
    QUOTA -->|excedeu| U429["429 Too Many Requests\napplication/problem+json"]

    QUOTA -->|dentro| CTRL["ContasController\n[Authorize]"]
    CTRL --> RESP["200 / 201 / 404 / 422\napplication/json\napplication/problem+json"]

    style U401 fill:#f8d7da,color:#721c24
    style U429 fill:#f8d7da,color:#721c24
    style RESP fill:#d4edda,color:#155724
```

## Contexto

O desafio cobra explicitamente *"como proteger dados sensíveis dos clientes"*.
Os endpoints estavam totalmente abertos — inaceitável para um sistema financeiro.
É preciso um mecanismo de autenticação proporcional ao escopo.

## Decisão

Autenticação por **API Key**: um `AuthenticationHandler` customizado lê o header
`X-Api-Key` e valida contra chaves configuradas. `[Authorize]` no `ContasController`;
`/saude` e Swagger ficam `[AllowAnonymous]`. Falha de auth retorna `401` em
ProblemDetails.

## Alternativas consideradas

- **JWT completo** — mais robusto e realista para identidade de cliente (claims,
  emissão/validação de token, ownership por `ClienteId`), mas exige um endpoint de
  auth e gestão de tokens que foge do domínio central (movimentações). Muito código
  para o valor demonstrado no escopo.
- **Sem auth (só hardening)** — deixaria os endpoints abertos; contraria o critério
  explícito do PDF.

## Consequências

- Autenticação serviço-a-serviço pragmática e stateless.
- Não há identidade de cliente nem autorização por ownership — documentado como
  evolução (JWT + validação de `ClienteId`).
- Simples de testar (header presente/ausente/ inválido → 200/401).
- As chaves ficam em configuração, não em banco (ver ADR-021).
