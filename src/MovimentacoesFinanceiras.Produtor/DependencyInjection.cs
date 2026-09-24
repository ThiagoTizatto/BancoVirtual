using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Produtor.RabbitMq;

namespace MovimentacoesFinanceiras.Produtor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProdutor(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IConexaoRabbitMq, ConexaoRabbitMq>();
        services.AddScoped<IPublicadorDeMovimentacoes, PublicadorRabbitMq>();

        return services;
    }
}
