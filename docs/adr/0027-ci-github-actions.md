# ADR-027 — CI GitHub Actions com -warnaserror

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-008, ADR-009

## Contexto

O código deve estar em repositório público no GitHub e compilar sem erros e
warnings. Sem CI, a garantia de que o build e os testes passam depende da máquina
de quem clona — e o requisito de "sem warnings" (ADR-008) não é provado publicamente.

## Decisão

Criar `.github/workflows/ci.yml` que roda em push/PR: `dotnet restore` → `build`
(com `-warnaserror`) → `test`. Os testes de integração usam Testcontainers, que
funciona no runner ubuntu (Docker já disponível — ADR-009).

## Alternativas consideradas

- **Sem CI** — deixa a garantia de build/teste implícita e não prova o requisito
  de "sem warnings" publicamente.
- **Azure DevOps / outro CI** — o repositório é no GitHub; Actions é a escolha
  natural, sem infra adicional.

## Consequências

- Prova pública, a cada push, de que o projeto compila sem warnings e passa nos
  testes — reforça o requisito eliminatório (ADR-008).
- Sinaliza maturidade de engenharia ao avaliador.
- Custo: manter o workflow; tempo de execução dos testes com Testcontainers no CI.
