# ADR-029 — Update atômico do saldo (elimina contenção sob concorrência)

**Status:** Aceito
**Data:** 2026-09-23
**Refina:** ADR-002 (concorrência otimista), ADR-005 (retry Polly)

## Contexto

Com SQLite, as escritas eram serializadas pelo WAL lock — os 3 retries do Polly
sempre resolviam os conflitos de concorrência otimista (`VersaoLinha`). Ao migrar
para PostgreSQL (ADR-009), a concorrência passou a ser **real**: N movimentações
simultâneas na mesma conta fazem *read-modify-write* do snapshot `saldo_atual`
protegido pelo token otimista, e colidem em massa (*thundering herd*). Um teste de
integração expôs o problema: 10 créditos concorrentes na mesma conta esgotavam os
3 retries e **alguns créditos retornavam 5xx e se perdiam**.

Num sistema financeiro isso é inaceitável: **um crédito nunca pode ser perdido por
contenção**. A causa raiz é tratar todas as movimentações como read-modify-write
otimista, quando na verdade:

- **Crédito é comutativo** — somar ao saldo não depende do valor atual (sem
  pré-condição). Não precisa de controle de concorrência otimista.
- **Débito tem pré-condição** — exige `saldo >= valor`. Precisa de atomicidade na
  verificação-e-escrita, mas não necessariamente de retry otimista.

## Decisão

Substituir, no caminho de escrita de movimentações, o read-modify-write otimista
por **updates atômicos no banco**, dentro de uma transação que também insere o
`Lancamento` (mantendo snapshot e ledger consistentes):

- **Crédito** — incremento incondicional:
  `UPDATE contas SET saldo_atual = saldo_atual + @valor WHERE id = @id`.
  0 linhas afetadas → conta não encontrada.
- **Débito** — decremento condicional:
  `UPDATE contas SET saldo_atual = saldo_atual - @valor WHERE id = @id AND saldo_atual >= @valor`.
  0 linhas afetadas → conta inexistente **ou** saldo insuficiente (distinguir
  consultando a existência da conta para lançar `SaldoInsuficienteException` vs
  `ContaNaoEncontradaException`).

A execução usa SQL parametrizado na **coluna** `saldo_atual` (via
`ExecuteSqlInterpolatedAsync`, sem risco de injeção). O `Lancamento` é inserido na
mesma transação (`BeginTransactionAsync` → update + insert → commit).

O agregado `Conta` continua expressando as operações e criando o `Lancamento`
(`Creditar`/`Debitar` seguem existindo para os testes de domínio e para a criação
do lançamento), mas a **mutação autoritativa do snapshot** ocorre no banco. A
pré-condição de débito é garantida autoritativamente pela cláusula `WHERE`; o
domínio fornece a exceção semântica quando o update afeta 0 linhas e a conta existe.

## Alternativas consideradas

- **Mais retries + jitter (Polly 3→10)** — band-aid: sob contenção maior ainda
  perde movimentações e infla latência. Resposta fraca ao eixo "alta demanda".
- **Lock pessimista (`SELECT ... FOR UPDATE`)** — correto e sem perda, mas serializa
  leituras, segura o lock durante todo o handler e cria risco de deadlock;
  contradiz ADR-002.

## Consequências

- **Positivas:** nenhum crédito/débito perdido sob concorrência; custo O(1) por
  operação independentemente da contenção na mesma conta (row lock dura só o
  UPDATE); restaura as asserções fortes dos testes de concorrência; resposta sólida
  a "alta demanda / falhas parciais".
- **Negativas / trade-offs:**
  - Parte da verificação de invariante (saldo ≥ valor no débito) migra do agregado
    para a cláusula SQL — tensão com DDD purista. Mitigado: o agregado ainda modela
    a operação e a exceção semântica; o banco é a fonte de verdade do saldo.
  - A mutação do snapshot passa a viver no repositório (Infraestrutura), não no
    agregado.
  - O retry Polly por `DbUpdateConcurrencyException` (ADR-005) deixa de ser
    necessário para movimentações — mantido apenas como defesa em profundidade ou
    removido do caminho de movimentação. `VersaoLinha` deixa de ser usado no write
    path de movimentação (permanece na entidade para outras atualizações futuras, ou
    pode ser removido em migration posterior — decisão adiada para não ampliar o
    escopo agora).
- **Idempotência:** inalterada — continua garantida pelo índice único filtrado e
  pela verificação da chave antes da escrita (ADR-028).

## Interação com o Plano 2 (SaldoAtual → Dinheiro VO)

O update atômico opera na **coluna `decimal` `saldo_atual`** via SQL parametrizado,
independente do tipo de domínio. Quando o Plano 2 transformar `Conta.SaldoAtual` em
`Dinheiro` (VO), as **leituras** materializam via value converter, mas o **write
path** continua usando a coluna diretamente — então a mudança do VO não quebra o
update atômico. O Plano 2 deve preservar esse caminho de escrita (não reintroduzir
read-modify-write do snapshot para movimentações).
