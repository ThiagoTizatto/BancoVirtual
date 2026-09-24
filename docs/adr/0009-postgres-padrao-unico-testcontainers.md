# ADR-009 — PostgreSQL como padrão único + Testcontainers nos testes

**Status:** Aceito
**Data:** 2026-09-23
**Substitui:** ADR-003 (SQLite para desenvolvimento)

## Contexto

O enunciado descreve um banco digital com *"lentidão, instabilidade em horários
de pico, alta demanda"*. O SQLite serializa escritas (single-writer) — exatamente
o gargalo que o desafio pede para resolver. Manter SQLite contradiz o cenário-alvo,
ainda que seja ótimo para zero-setup do avaliador.

## Decisão

Adotar **PostgreSQL como padrão único** (dev, testes e produção), provisionado via
`docker-compose`. Nos testes de integração, subir um container Postgres efêmero por
instância via `Testcontainers.PostgreSql`, substituindo o SQLite em arquivo temporário
da `AplicacaoFactory`.

## Alternativas consideradas

- **Postgres + SQLite dual (SQLite só em testes)** — zero dependência de Docker no
  teste, mas testa contra um engine diferente do de produção; concorrência e SQL
  divergem do real. Contradiz "padrão único".
- **Manter SQLite** — menor esforço, mas mantém a incoerência "alta demanda vs
  single-writer" e enfraquece a narrativa arquitetural.
- **Postgres compartilhado nos testes** — estado compartilhado gera flakiness e
  não isola instâncias.

## Consequências

- Coerência com o cenário de alta demanda; concorrência otimista testada contra o
  engine real (MVCC do Postgres).
- O avaliador precisa de Docker para rodar (perde o "zero setup") — mitigado por
  `docker-compose up` documentado e um único comando.
- A CI (runner ubuntu) já tem Docker, então Testcontainers roda sem config extra.
- A abstração `IContaRepository` + EF já isolava o domínio, então o custo da troca
  fica concentrado na Infraestrutura.
