using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;

namespace Api.Testes;

public class AplicacaoFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"integracao-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(servicos =>
        {
            // Remove o DbContext e DbContextOptions registrados pelo Program.cs (SQLite de produção)
            var descritores = servicos
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<BancoDadosContext>) ||
                    d.ServiceType == typeof(BancoDadosContext))
                .ToList();

            foreach (var descritor in descritores)
                servicos.Remove(descritor);

            // Usa SQLite com arquivo temporário — garante concurrency tokens corretos
            // e isolamento total entre instâncias de AplicacaoFactory
            servicos.AddDbContext<BancoDadosContext>(opcoes =>
                opcoes
                    .UseSqlite($"Data Source={_dbPath}")
                    .EnableSensitiveDataLogging());
        });
    }

    public async Task InitializeAsync()
    {
        using var escopo = Services.CreateScope();
        var ctx = escopo.ServiceProvider.GetRequiredService<BancoDadosContext>();
        await ctx.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        // Tenta remover o arquivo de banco e arquivos auxiliares do SQLite (WAL, SHM)
        // Erros de IO são ignorados — o SO limpará os arquivos temporários
        foreach (var extensao in new[] { "", "-wal", "-shm" })
        {
            var arquivo = _dbPath + extensao;
            try { if (File.Exists(arquivo)) File.Delete(arquivo); }
            catch (IOException) { /* arquivo ainda em uso — será limpo pelo SO */ }
        }
    }
}
