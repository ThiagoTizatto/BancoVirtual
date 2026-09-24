# ADR-021 — API Keys em configuração (vs tabela)

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-018, ADR-022

## Contexto

Definida a autenticação por API Key (ADR-018), resta decidir onde as chaves válidas
são armazenadas e validadas.

## Decisão

Armazenar a lista de chaves válidas em **configuração** (via `user-secrets` em dev,
variáveis de ambiente em Docker/prod — ver ADR-022). A validação é stateless, sem
tabela nem consulta ao banco.

## Alternativas consideradas

- **Tabela no banco** — entidade `ApiKey` persistida (hash da chave, cliente
  associado, revogação por linha), mais realista e com escopo por cliente. Porém
  adiciona um subdomínio de gestão de credenciais que foge do núcleo (movimentações)
  e infla o modelo — desproporcional ao escopo do desafio.

## Consequências

- Domínio focado em movimentações, sem subdomínio de credenciais.
- Validação stateless e rápida (sem I/O de banco no caminho de auth).
- Rotação de chave exige redeploy/reconfiguração (sem revogação granular em runtime)
  — aceitável para auth serviço-a-serviço; tabela dedicada documentada como evolução.
