using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia.Repositories;

namespace MovimentacoesFinanceiras.Infraestrutura;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<BancoDadosContext>(opcoes =>
            opcoes.UseNpgsql(configuration.GetConnectionString("Postgres")
                ?? "Host=localhost;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres"));

        services.AddScoped<IContaRepository, ContaRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<BancoDadosContext>("banco-de-dados");

        return services;
    }

    public static async Task AplicarMigracoesAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        using var escopo = serviceProvider.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<BancoDadosContext>();
        await contexto.Database.MigrateAsync(cancellationToken);
    }
}
