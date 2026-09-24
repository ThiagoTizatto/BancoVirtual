using System.Diagnostics.Metrics;

namespace MovimentacoesFinanceiras.Aplicacao.Metricas;

public class MetricasMovimentacao
{
    public const string NomeMeter = "MovimentacoesFinanceiras";

    private readonly Meter _meter;
    private readonly Counter<long> _movimentacoes;

    public MetricasMovimentacao(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        _meter = meterFactory.Create(NomeMeter);
        _movimentacoes = _meter.CreateCounter<long>(
            "movimentacoes.total",
            description: "Total de movimentações registradas, por tipo.");
    }

    public void RegistrarMovimentacao(string tipo) =>
        _movimentacoes.Add(1, new KeyValuePair<string, object?>("tipo", tipo));
}
