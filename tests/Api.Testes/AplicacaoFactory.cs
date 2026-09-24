using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Api.Autenticacao;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;
using Testcontainers.PostgreSql;

namespace Api.Testes;

public class AplicacaoFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiKeyTeste = "chave-de-teste";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("movimentacoes_teste")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public HttpClient CriarClienteAutenticado()
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Add(ApiKeyOptions.NomeCabecalho, ApiKeyTeste);
        return cliente;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKeys:0"] = ApiKeyTeste
            });
        });

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
