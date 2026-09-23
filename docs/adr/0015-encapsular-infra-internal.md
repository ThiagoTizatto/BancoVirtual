# ADR-015 — Encapsular infra com internal + InternalsVisibleTo

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-007

## Contexto

O `ContasController` recebia `BancoDadosContext` e `IContaRepository` por injeção,
e o método `CriarConta` acessava o `DbContext` diretamente — furando o CQRS que o
resto do sistema segue. Infraestrutura vazava na camada de apresentação, violando
o fluxo de dependência da Clean Architecture.

## Decisão

- `BancoDadosContext` e `ContaRepository` viram `internal`, registrados apenas
  dentro de `AddInfraestrutura` (ADR-007).
- `ContasController` passa a depender exclusivamente de `IMediator`.
- A criação de conta vira `CriarContaCommand` (via MediatR), eliminando o acesso
  direto ao `DbContext`.
- `InternalsVisibleTo` para `Api.Testes`, onde a `AplicacaoFactory` precisa
  substituir o `DbContext` nos testes de integração.

## Alternativas consideradas

- **Manter tipos `public`** — mais simples, mas não impede novos vazamentos; a Api
  continuaria podendo referenciar tipos da Infraestrutura.
- **Interface pública + implementação internal** — já é o caso de `IContaRepository`
  (interface no domínio); o ganho aqui é tornar a *implementação* e o `DbContext`
  inacessíveis fora da Infraestrutura.

## Consequências

- A Api referencia apenas Aplicacao e Dominio — fluxo de dependência verificável
  pelo compilador, não só por convenção.
- CQRS uniforme: toda escrita passa por Command/Handler.
- Custo: `InternalsVisibleTo` acopla a Infraestrutura ao assembly de teste (aceitável
  e padrão para testes de integração que substituem o contexto).
