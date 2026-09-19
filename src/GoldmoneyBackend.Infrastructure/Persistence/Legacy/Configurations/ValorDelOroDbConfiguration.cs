using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class ValorDelOroDbConfiguration : IEntityTypeConfiguration<ValorDelOroDb>
{
    public void Configure(EntityTypeBuilder<ValorDelOroDb> builder)
    {
        builder.ToTable("VALOR_DEL_ORO");
        builder.HasKey(x => new { x.CodigoEmpresa, x.StatusCalidad, x.Kilataje });

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.StatusCalidad)
            .HasColumnName("status_calidad")
            .HasMaxLength(1)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.Kilataje)
            .HasColumnName("kilataje")
            .HasPrecision(2, 0)
            .IsRequired();
        builder.Property(x => x.MaximoValor)
            .HasColumnName("maximo_valor")
            .HasColumnType("money");
        builder.Property(x => x.MinimoValor)
            .HasColumnName("minimo_valor")
            .HasColumnType("money");
    }
}
