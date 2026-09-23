using Microsoft.EntityFrameworkCore;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Infraestrutura.Persistencia.Repositories;

public class ContaRepository(BancoDadosContext contexto) : IContaRepository
{
    private readonly BancoDadosContext _contexto = contexto;

    public async Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _contexto.Contas.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Lancamento?> ObterLancamentoPorChaveIdempotenciaAsync(string chave, CancellationToken cancellationToken = default)
        => await _contexto.Lancamentos.FirstOrDefaultAsync(l => l.ChaveIdempotencia == chave, cancellationToken);

    public async Task AdicionarAsync(Conta conta, CancellationToken cancellationToken = default)
        => await _contexto.Contas.AddAsync(conta, cancellationToken);

    public async Task AdicionarLancamentoAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
        => await _contexto.Lancamentos.AddAsync(lancamento, cancellationToken);

    public async Task SalvarAsync(CancellationToken cancellationToken = default)
        => await _contexto.SaveChangesAsync(cancellationToken);

    public async Task<decimal> ConsultarSaldoEmAsync(Guid contaId, DateTime dataReferencia, CancellationToken cancellationToken = default)
    {
        var lancamentos = await _contexto.Lancamentos
                .Where(l => l.ContaId == contaId && l.CriadoEm <= dataReferencia)
                .ToListAsync(cancellationToken);

        return lancamentos.Sum(l => l.Tipo == TipoLancamento.Credito ? l.Valor : -l.Valor);
    }

    public async Task<(IReadOnlyList<Lancamento> Itens, int Total)> ListarLancamentosAsync(
            Guid contaId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        var query = _contexto.Lancamentos
                .Where(l => l.ContaId == contaId)
                .OrderByDescending(l => l.CriadoEm);

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<Lancamento> RegistrarMovimentacaoAsync(
        Guid contaId, TipoLancamento tipo, Dinheiro valor,
        string? descricao, string? chaveIdempotencia, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valor);

        await using var transacao = await _contexto.Database.BeginTransactionAsync(cancellationToken);

        var quantia = valor.Quantia;

        var linhasAfetadas = tipo == TipoLancamento.Credito
            ? await _contexto.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE contas SET saldo_atual = saldo_atual + {quantia} WHERE id = {contaId}",
                cancellationToken)
            : await _contexto.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE contas SET saldo_atual = saldo_atual - {quantia} WHERE id = {contaId} AND saldo_atual >= {quantia}",
                cancellationToken);

        if (linhasAfetadas == 0)
        {
            var contaExiste = await _contexto.Contas.AnyAsync(c => c.Id == contaId, cancellationToken);
            if (!contaExiste)
                throw new ContaNaoEncontradaException(contaId);

            var saldoAtual = await _contexto.Contas
                .Where(c => c.Id == contaId)
                .Select(c => c.SaldoAtual)
                .FirstAsync(cancellationToken);
            throw new SaldoInsuficienteException(saldoAtual.Quantia, valor);
        }

        var lancamento = tipo == TipoLancamento.Credito
            ? Lancamento.CriarCredito(contaId, quantia, descricao, chaveIdempotencia)
            : Lancamento.CriarDebito(contaId, quantia, descricao, chaveIdempotencia);

        await _contexto.Lancamentos.AddAsync(lancamento, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);

        await transacao.CommitAsync(cancellationToken);
        return lancamento;
    }
}
