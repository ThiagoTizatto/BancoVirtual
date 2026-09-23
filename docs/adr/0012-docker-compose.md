# ADR-012 — Docker/docker-compose como caminho principal

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

Com Postgres como padrão único (ADR-009), o avaliador precisa de um banco rodando.
O requisito obrigatório pede *"README com instruções claras de como rodar a
aplicação localmente"*. Sem containerização, o setup exige instalar e configurar
Postgres manualmente.

## Decisão

Fornecer `Dockerfile` multi-stage (SDK build → runtime aspnet) e `docker-compose.yml`
com os serviços `api` + `postgres` (volume persistente, healthcheck, `depends_on`).
Connection string e API Key injetadas via variáveis de ambiente no compose.
`docker-compose up` vira o caminho principal de execução no README.

## Alternativas consideradas

- **Instruções manuais de instalação do Postgres** — frágil, dependente do ambiente
  do avaliador, contraria "instruções claras".
- **Só Dockerfile, sem compose** — não orquestra o banco junto; o avaliador ainda
  precisaria subir o Postgres à parte.

## Consequências

- Um comando sobe API + banco, íntegro e reproduzível.
- Habilita o Postgres do ADR-009 sem setup manual.
- Custo: exige Docker instalado (aceitável e padrão de mercado).
- Alinha o ambiente local ao de produção (paridade dev/prod).
