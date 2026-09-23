namespace MovimentacoesFinanceiras.Dominio.Contas;

public interface IContaRepository
{
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lancamento?> ObterLancamentoPorChaveIdempotenciaAsync(string chave, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Conta conta, CancellationToken cancellationToken = default);
    Task AdicionarLancamentoAsync(Lancamento lancamento, CancellationToken cancellationToken = default);
    Task SalvarAsync(CancellationToken cancellationToken = default);
    Task<decimal> ConsultarSaldoEmAsync(Guid contaId, DateTime dataReferencia, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Lancamento> Itens, int Total)> ListarLancamentosAsync(
        Guid contaId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);
    Task<Lancamento> RegistrarMovimentacaoAsync(
        Guid contaId, TipoLancamento tipo, Dinheiro valor,
        string? descricao, string? chaveIdempotencia, CancellationToken cancellationToken = default);
}
