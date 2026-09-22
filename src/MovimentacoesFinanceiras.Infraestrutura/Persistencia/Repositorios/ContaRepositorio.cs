using Microsoft.EntityFrameworkCore;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Infraestrutura.Persistencia.Repositorios;

public class ContaRepositorio : IContaRepositorio
{
    private readonly ContextoBancoDados _contexto;

    public ContaRepositorio(ContextoBancoDados contexto) => _contexto = contexto;

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
}
