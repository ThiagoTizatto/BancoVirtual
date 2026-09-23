namespace MovimentacoesFinanceiras.Dominio.Contas;

public sealed class Conta
{
    private readonly List<Lancamento> _lancamentos = [];

    private Conta() { }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public decimal SaldoAtual { get; private set; }
    public Guid VersaoLinha { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyList<Lancamento> Lancamentos => _lancamentos.AsReadOnly();

    public static Conta Criar(Guid clienteId) => new()
    {
        Id = Guid.NewGuid(),
        ClienteId = clienteId,
        SaldoAtual = 0m,
        VersaoLinha = Guid.NewGuid(),
        CriadoEm = DateTime.UtcNow
    };

    public Lancamento Creditar(Dinheiro valor, string? descricao, string? chaveIdempotencia = null)
    {
        ArgumentNullException.ThrowIfNull(valor);
        SaldoAtual += valor.Quantia;
        VersaoLinha = Guid.NewGuid();
        var lancamento = Lancamento.CriarCredito(Id, valor.Quantia, descricao, chaveIdempotencia);
        _lancamentos.Add(lancamento);
        return lancamento;
    }

    public Lancamento Debitar(Dinheiro valor, string? descricao, string? chaveIdempotencia = null)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (valor.Quantia > SaldoAtual)
            throw new Excecoes.SaldoInsuficienteException(SaldoAtual, valor);
        SaldoAtual -= valor.Quantia;
        VersaoLinha = Guid.NewGuid();
        var lancamento = Lancamento.CriarDebito(Id, valor.Quantia, descricao, chaveIdempotencia);
        _lancamentos.Add(lancamento);
        return lancamento;
    }
}
