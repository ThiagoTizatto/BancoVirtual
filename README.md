# Sistema de Movimentações Financeiras

Sistema bancário para registro de movimentações financeiras (créditos e débitos) e consulta de saldo — atual e histórico (_point-in-time_) — construído como solução para o desafio técnico de arquiteto de software.

## Como rodar

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Executar a aplicação

```bash
git clone <url-do-repositorio>
cd desafio-banco
dotnet run --project src/MovimentacoesFinanceiras.Api
```

A API sobe em `http://localhost:5095`. O Swagger está disponível na raiz: `http://localhost:5095`.

O banco SQLite (`movimentacoes.db`) é criado automaticamente na primeira execução — não há passo de migração manual.

### Executar os testes

```bash
dotnet test
```

Suíte atual: **31 testes** (18 de domínio + 13 de API, cobrindo integração e concorrência).

---

## Exemplos de uso

### Criar uma conta

```bash
curl -X POST http://localhost:5095/contas \
  -H "Content-Type: application/json" \
  -d '{"clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
```

### Registrar um crédito

```bash
curl -X POST http://localhost:5095/contas/{id}/movimentacoes \
  -H "Content-Type: application/json" \
  -d '{"tipo": "Credito", "valor": 100.00, "descricao": "Depósito inicial"}'
```

### Registrar um débito idempotente

O cabeçalho `Idempotency-Key` garante que retries do cliente não gerem lançamentos duplicados.

```bash
curl -X POST http://localhost:5095/contas/{id}/movimentacoes \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 8f3a1c2e-..." \
  -d '{"tipo": "Debito", "valor": 30.00, "descricao": "Saque"}'
```

### Consultar saldo atual

```bash
curl http://localhost:5095/contas/{id}/saldo
```

### Consultar saldo em ponto no tempo

```bash
curl "http://localhost:5095/contas/{id}/saldo?em=2025-01-15T12:00:00Z"
```

### Listar extrato paginado

```bash
curl "http://localhost:5095/contas/{id}/movimentacoes?pagina=1&tamanhoPagina=20"
```

### Health check

```bash
curl http://localhost:5095/saude
```

---

## Arquitetura

A solução usa **CQRS + Ledger append-only** dentro de uma **Clean Architecture** com **DDD**. A documentação completa das decisões arquiteturais está em:

- [`docs/superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md`](docs/superpowers/specs/2026-09-22-movimentacoes-financeiras-design.md) — Design doc completo com ADRs

### Camadas

```
Dominio         → agregado Conta, Lancamento, Dinheiro (VO), regras de negócio puras
Aplicacao       → CQRS via MediatR, validação (FluentValidation), retry (Polly)
Infraestrutura  → EF Core + SQLite, ContaRepositorio
Api             → ASP.NET Core, controladores, middlewares, Serilog, Swagger
```

O fluxo de dependências aponta sempre para dentro (`Api → Aplicacao → Dominio`); o domínio não conhece nenhuma dependência externa.

### Decisões principais

| Decisão | Justificativa |
|---|---|
| Ledger append-only | Rastreabilidade imutável; saldo histórico via soma dos lançamentos sem mecanismo extra |
| CQRS | Leitura e escrita com modelos e caminhos independentes |
| Concorrência otimista + Polly retry | Sem bloqueio de leituras; conflitos de versão absorvidos internamente |
| Idempotência via `Idempotency-Key` | Retries do cliente nunca geram lançamentos duplicados |
| Snapshot `saldo_atual` + ledger | Consulta de saldo atual em O(1); ledger garante consistência e auditoria |
| SQLite | Zero dependências externas para rodar localmente |

### O que ficou de fora (e por quê)

- **Autenticação JWT** — fora do escopo do desafio; ponto de extensão natural documentado
- **Snapshot periódico de saldo histórico** — necessário para históricos de anos com alto volume; para o escopo, a soma do ledger resolve
- **Circuit breaker** — relevante em produção; Polly já está no projeto como dependência
