using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;
using Testcontainers.PostgreSql;

namespace Api.Testes;

public class AplicacaoFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("movimentacoes_teste")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(servicos =>
        {
            var descritores = servicos
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<BancoDadosContext>) ||
                    d.ServiceType == typeof(BancoDadosContext))
                .ToList();

            foreach (var descritor in descritores)
                servicos.Remove(descritor);

            servicos.AddDbContext<BancoDadosContext>(opcoes =>
                opcoes
                    .UseNpgsql(_postgres.GetConnectionString())
                    .EnableSensitiveDataLogging());
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var escopo = Services.CreateScope();
        var ctx = escopo.ServiceProvider.GetRequiredService<BancoDadosContext>();
        await ctx.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
