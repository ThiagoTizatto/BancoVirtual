using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Infraestrutura.Persistencia.Configurations;

public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.ContaId).HasColumnName("conta_id").IsRequired();
        builder.Property(l => l.Valor).HasColumnName("valor").HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.Tipo).HasColumnName("tipo").HasConversion<string>().IsRequired();
        builder.Property(l => l.Descricao).HasColumnName("descricao").HasMaxLength(255);
        builder.Property(l => l.ChaveIdempotencia).HasColumnName("chave_idempotencia").HasMaxLength(64);
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(l => new { l.ContaId, l.CriadoEm });

        // SQLite trata múltiplos NULLs como valores distintos nativamente,
        // portanto o índice único já garante a semântica de unicidade apenas
        // para valores não-nulos — sem necessidade de filtro explícito.
        builder.HasIndex(l => l.ChaveIdempotencia).IsUnique();
    }
}
