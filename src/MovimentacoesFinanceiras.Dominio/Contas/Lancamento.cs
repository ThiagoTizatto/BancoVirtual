namespace MovimentacoesFinanceiras.Dominio.Contas;

public sealed class Lancamento
{
    private Lancamento() { }

    public Guid Id { get; private set; }
    public Guid ContaId { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public string? Descricao { get; private set; }
    public string? ChaveIdempotencia { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public static Lancamento CriarCredito(Guid contaId, decimal valor, string? descricao, string? chaveIdempotencia = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Valor = valor,
            Tipo = TipoLancamento.Credito,
            Descricao = descricao,
            ChaveIdempotencia = chaveIdempotencia,
            CriadoEm = DateTime.UtcNow
        };

    public static Lancamento CriarDebito(Guid contaId, decimal valor, string? descricao, string? chaveIdempotencia = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Valor = valor,
            Tipo = TipoLancamento.Debito,
            Descricao = descricao,
            ChaveIdempotencia = chaveIdempotencia,
            CriadoEm = DateTime.UtcNow
        };
}
