# ADR-022 — Estratégia de secrets em camadas

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-021

## Contexto

A connection string e a API Key estavam em `appsettings.json` em texto plano —
segredos versionados no repositório, prática insegura para dados financeiros.

## Decisão

Estratégia de secrets em camadas por ambiente:

- **Desenvolvimento:** `dotnet user-secrets` (fora do controle de versão).
- **Docker/produção:** variáveis de ambiente (injetadas pelo `docker-compose` ou
  pelo orquestrador).
- `appsettings.json` mantém apenas placeholders/estrutura, sem valores sensíveis.

O destino de produção recomendado (Azure Key Vault / AWS Secrets Manager) fica
documentado como evolução.

## Alternativas consideradas

- **Manter em `appsettings.json`** — simples, mas expõe segredos no repositório.
- **Vault desde já** — mais seguro em produção, mas adiciona dependência de infra
  externa desproporcional ao escopo local do desafio.

## Consequências

- Nenhum segredo versionado no repositório.
- Paridade de mecanismo entre Docker local e produção (env vars).
- Caminho de evolução para Vault sem refatorar a leitura de configuração
  (o .NET `IConfiguration` abstrai a fonte).
