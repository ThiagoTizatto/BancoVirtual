using System.Text.Json;
using System.Text.Json.Serialization;

namespace MovimentacoesFinanceiras.Mensagens;

public static class MensagensSerializerOptions
{
    public static readonly JsonSerializerOptions Padrao = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
