using Microsoft.Extensions.DependencyInjection;

namespace MovimentacoesFinanceiras.Dominio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDominio(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
