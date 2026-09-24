namespace MovimentacoesFinanceiras.Dominio.Contas;

public sealed class Conta
{
    private readonly List<Lancamento> _lancamentos = [];

    private Conta() { }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public Dinheiro SaldoAtual { get; private set; } = Dinheiro.Zero;
    public Guid VersaoLinha { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyList<Lancamento> Lancamentos => _lancamentos.AsReadOnly();

    public static Conta Criar(Guid clienteId) => new()
    {
        Id = Guid.NewGuid(),
        ClienteId = clienteId,
        SaldoAtual = Dinheiro.Zero,
        VersaoLinha = Guid.NewGuid(),
        CriadoEm = DateTime.UtcNow
    };

    public Lancamento Creditar(Dinheiro valor, string? descricao, string? chaveIdempotencia = null)
    {
        ArgumentNullException.ThrowIfNull(valor);
        SaldoAtual += valor;
        VersaoLinha = Guid.NewGuid();
        var lancamento = Lancamento.CriarCredito(Id, valor.Quantia, descricao, chaveIdempotencia);
        _lancamentos.Add(lancamento);
        return lancamento;
    }

    public Lancamento Debitar(Dinheiro valor, string? descricao, string? chaveIdempotencia = null)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (valor > SaldoAtual)
            throw new Excecoes.SaldoInsuficienteException(SaldoAtual.Quantia, valor);
        SaldoAtual -= valor;
        VersaoLinha = Guid.NewGuid();
        var lancamento = Lancamento.CriarDebito(Id, valor.Quantia, descricao, chaveIdempotencia);
        _lancamentos.Add(lancamento);
        return lancamento;
    }
}
