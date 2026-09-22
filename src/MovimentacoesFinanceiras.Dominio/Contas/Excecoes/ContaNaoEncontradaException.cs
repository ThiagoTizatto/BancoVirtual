namespace MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

public sealed class ContaNaoEncontradaException : Exception
{
    public ContaNaoEncontradaException(Guid contaId)
        : base($"Conta {contaId} não encontrada.")
    {
        ContaId = contaId;
    }

    public Guid ContaId { get; }
}
