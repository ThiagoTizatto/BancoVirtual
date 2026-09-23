# ADR-007 — Inicializadores de camada e composition root magra

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O `Program.cs` concentrava o registro de DI de todas as camadas. Como efeito,
o `ContasController` recebia `BancoDadosContext` e `IContaRepository` diretamente
— infraestrutura vazando na camada de apresentação. O wiring espalhado dificulta
enxergar de quem é a responsabilidade de montar cada camada e contamina o
startup com detalhes de implementação.

## Decisão

Cada projeto expõe um único método de extensão `Add<Camada>()` em um
`DependencyInjection.cs` na raiz, dono de todo o registro de DI daquela camada:
`AddDominio()`, `AddAplicacao()`, `AddInfraestrutura(IConfiguration)`, `AddApi()`.

O `Program.cs` vira uma *composition root* magra que apenas encadeia os
inicializadores e ordena o pipeline HTTP. Não conhece *como* cada camada se monta.

```csharp
builder.Services
    .AddDominio()
    .AddAplicacao()
    .AddInfraestrutura(builder.Configuration)
    .AddApi();
```

## Alternativas consideradas

- **Manter registro no `Program.cs`** — menos arquivos, mas mantém o vazamento
  de infra e um startup extenso e acoplado.
- **`Add<Camada>Services()`** — mais explícito, porém verboso e menos alinhado à
  convenção da BCL (`AddMvc`, `AddAuthentication`).

## Consequências

- Startup declarativo: `Program.cs` diz *que* cada camada se monta, não *como*.
- Cada camada é responsável pelo próprio wiring — coerente com Clean Architecture.
- Habilita tornar `DbContext`/repositories `internal` (ver ADR-015).
- Trocar provider, adicionar circuit breaker ou métricas fica contido no
  inicializador da camada certa.
