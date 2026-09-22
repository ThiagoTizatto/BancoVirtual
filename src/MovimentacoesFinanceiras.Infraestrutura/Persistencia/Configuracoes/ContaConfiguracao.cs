using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Infraestrutura.Persistencia.Configuracoes;

public class ContaConfiguracao : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("contas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.ClienteId).HasColumnName("cliente_id").IsRequired();
        builder.Property(c => c.SaldoAtual).HasColumnName("saldo_atual").HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.VersaoLinha)
               .HasColumnName("versao_linha")
               .HasColumnType("TEXT")
               .HasConversion(g => g.ToString(), s => Guid.Parse(s))
               .IsConcurrencyToken()
               .IsRequired();
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(c => c.ClienteId);

        builder.HasMany(c => c.Lancamentos)
               .WithOne()
               .HasForeignKey(l => l.ContaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Lancamentos).HasField("_lancamentos");
    }
}
