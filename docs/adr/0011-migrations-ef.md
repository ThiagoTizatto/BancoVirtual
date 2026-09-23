# ADR-011 — Migrations EF (vs EnsureCreated)

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O schema era criado via `EnsureCreatedAsync` no startup, e alterações de modelo
exigiam apagar o `.db` e reiniciar. Para um *"ambiente financeiro real"*, isso é
sinal de imaturidade: não há versionamento de schema, nem caminho de evolução
sem perda de dados.

## Decisão

Adotar EF Core Migrations. Criar a migration inicial (`InicialPostgres`) e aplicar
`db.Database.MigrateAsync()` no startup. O ambiente `"Testing"` controla a aplicação
via `AplicacaoFactory` (container efêmero, ver ADR-009).

## Alternativas consideradas

- **Manter `EnsureCreatedAsync`** — simples, mas sem versionamento nem evolução
  incremental; incompatível com produção.
- **Scripts SQL manuais** — controle total, mas perde a integração com o modelo EF
  e exige manutenção paralela.

## Consequências

- Schema versionado e evoluível sem perda de dados.
- Histórico de mudanças de schema rastreável no repositório.
- Custo: fluxo de trabalho com `dotnet ef migrations add` a cada mudança de modelo.
- Remove a instrução "apague o `.db`" do README/CLAUDE.md.
