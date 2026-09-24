using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Worker.Processamento;
using MovimentacoesFinanceiras.Worker.RabbitMq;

namespace MovimentacoesFinanceiras.Worker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorker(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IConexaoRabbitMq, ConexaoRabbitMq>();
        services.AddSingleton<TopologiaDeclarator>();
        services.AddTransient<ProcessadorDeMovimentacao>();
        services.AddHostedService<ConsumidorDeMovimentacoes>();

        return services;
    }
}
