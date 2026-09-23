using System.Text.Json.Serialization;

namespace MovimentacoesFinanceiras.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddControllers()
            .AddJsonOptions(opcoes =>
                opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new()
            {
                Title = "API de Movimentações Financeiras",
                Description = "Registra movimentações financeiras e consulta saldos de contas bancárias. " +
                              "Utiliza CQRS com ledger append-only para rastreabilidade completa.",
                Version = "v1"
            });
        });

        return services;
    }
}
