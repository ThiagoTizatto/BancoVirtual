# ADR-008 — TreatWarningsAsErrors / build sob trava

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

O desafio técnico exige, como requisito obrigatório eliminatório, que o
*"código fonte compile sem erros e warnings"*. Não havia `Directory.Build.props`
nem `TreatWarningsAsErrors` — o build podia passar com warnings silenciosos,
o que reprova a entrega por acaso, não por mérito.

## Decisão

Criar `Directory.Build.props` na raiz, herdado por todos os `.csproj`, com:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<Nullable>enable</Nullable>
<AnalysisMode>All</AnalysisMode>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

Centralizar `TargetFramework`/`Nullable`/`ImplicitUsings` no props e removê-los
dos `.csproj` individuais. Corrigir todos os warnings expostos.

## Alternativas consideradas

- **Confiar no build limpo atual** — frágil; qualquer edição futura pode
  introduzir warning sem falhar o build, quebrando o requisito.
- **`#pragma warning disable` pontual** — mascara problemas em vez de resolvê-los.

## Consequências

- O requisito eliminatório passa a ser *provado*, não cumprido por acaso.
- `AnalysisMode=All` liga analyzers Roslyn (regras CA) — pode expor passivo que
  precisa ser corrigido no mesmo passo.
- Configuração única e herdada evita divergência entre projetos.
- A CI reforça a trava publicamente com `dotnet build -warnaserror` (ver ADR-027).
