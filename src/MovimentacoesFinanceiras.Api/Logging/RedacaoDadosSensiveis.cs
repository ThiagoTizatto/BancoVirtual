using Serilog.Core;
using Serilog.Events;

namespace MovimentacoesFinanceiras.Api.Logging;

public sealed class RedacaoDadosSensiveis : IDestructuringPolicy
{
    private static readonly HashSet<string> CamposSensiveis = new(StringComparer.OrdinalIgnoreCase)
    {
        "valor", "saldo", "saldoAtual", "quantia", "descricao"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(propertyValueFactory);

        var estruturado = value.GetType().GetProperties()
            .Select(p =>
            {
                var conteudo = CamposSensiveis.Contains(p.Name)
                    ? (object?)"[REDIGIDO]"
                    : p.GetValue(value);
                return new LogEventProperty(p.Name, propertyValueFactory.CreatePropertyValue(conteudo, true));
            });

        result = new StructureValue(estruturado);
        return true;
    }
}
