namespace MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

public sealed class ContaNaoEncontradaException : Exception
{
    public ContaNaoEncontradaException()
        : base("Conta não encontrada.")
    {
    }

    public ContaNaoEncontradaException(string message)
        : base(message)
    {
    }

    public ContaNaoEncontradaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ContaNaoEncontradaException(Guid contaId)
        : base($"Conta {contaId} não encontrada.")
    {
        ContaId = contaId;
    }

    public Guid ContaId { get; }
}
