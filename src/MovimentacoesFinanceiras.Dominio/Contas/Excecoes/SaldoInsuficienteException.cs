namespace MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

public sealed class SaldoInsuficienteException : Exception
{
    public SaldoInsuficienteException(decimal saldoAtual, Dinheiro valorSolicitado)
        : base($"Saldo insuficiente. Disponível: R$ {saldoAtual:N2}, solicitado: {valorSolicitado}.")
    {
        SaldoAtual = saldoAtual;
        ValorSolicitado = valorSolicitado;
    }

    public decimal SaldoAtual { get; }
    public Dinheiro ValorSolicitado { get; }
}
