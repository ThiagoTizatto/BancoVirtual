using Microsoft.EntityFrameworkCore;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Infraestrutura.Persistencia;

public class BancoDadosContext : DbContext
{
    public BancoDadosContext(DbContextOptions<BancoDadosContext> options) : base(options) { }

    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancoDadosContext).Assembly);
    }
}
