# ADR-014 — SaldoAtual como Dinheiro + Reconstituir

**Status:** Aceito
**Data:** 2026-09-23
**Relacionado:** ADR-013

## Contexto

O Value Object `Dinheiro` existia mas era subutilizado: só validava a entrada
(`De()` exige `> 0`) e não tinha aritmética. `Conta.SaldoAtual` era `decimal` cru,
e as regras de negócio acessavam `.Quantia` diretamente. É a crítica de modelagem
DDD mais provável de um avaliador — VO anêmico.

## Decisão

Enriquecer `Dinheiro` com `Zero`, operadores aritméticos (`+`, `-`) e de comparação
(`>`, `<`, `>=`, `<=`). Transformar `Conta.SaldoAtual` em `Dinheiro`. Adicionar um
factory `internal static Dinheiro Reconstituir(decimal)` para rehidratação de
persistência, exposto à Infraestrutura via `InternalsVisibleTo`.

```csharp
public static Dinheiro Zero { get; } = new(0m);
public static Dinheiro De(decimal q) => q <= 0 ? throw ... : new(q);   // entrada
internal static Dinheiro Reconstituir(decimal q) => new(q);            // persistência
```

## Alternativas consideradas

- **Manter `decimal` cru** — menos churn, mas mantém o VO anêmico e a crítica DDD.
- **`Dinheiro` rico, saldo `decimal`** — meio-termo; VO ganha aritmética mas o saldo
  não se beneficia, e a comparação `valor > SaldoAtual` continua crua.
- **`De()` aceitar zero** — quebraria a invariante de entrada (não se movimenta zero);
  por isso a separação `De()` (entrada, `> 0`) vs `Reconstituir` (rehidratação,
  qualquer valor) vs `Zero` (saldo inicial).

## Consequências

- Regras de negócio expressivas: `SaldoAtual + valor`, `valor > SaldoAtual`.
- A invariante de saldo não-negativo permanece na `Conta` (guard clause), não no VO.
- `Reconstituir` é `internal` — o EF materializa sem violar a invariante de entrada;
  padrão consagrado de rehidratação de VO em DDD.
- Custo: value converter no EF (`d => d.Quantia`, `v => Dinheiro.Reconstituir(v)`)
  e ajuste dos testes de domínio.
