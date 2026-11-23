using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class HDetalleContratoDbConfiguration : IEntityTypeConfiguration<HDetalleContratoDb>
{
    public void Configure(EntityTypeBuilder<HDetalleContratoDb> builder)
    {
        builder.ToTable("H_DETALLES_CONTRATOS");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato, x.NumeroDetalle });

        builder.Property(x => x.CodigoEmpresa).HasColumnName("codigo_empresa").HasMaxLength(2).IsUnicode(false).IsRequired();
        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.NumeroDetalle).HasColumnName("numero_detalle");
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300).IsUnicode(false);
        builder.Property(x => x.Kilates).HasColumnName("kilates").HasColumnType("decimal(2,0)");
        builder.Property(x => x.Peso).HasColumnName("peso").HasColumnType("decimal(6,2)");
        builder.Property(x => x.CodigoReloj).HasColumnName("codigo_reloj");
        builder.Property(x => x.CantidadProducto).HasColumnName("cantidad_producto");
        builder.Property(x => x.CodigoCategoriaPrenda).HasColumnName("codigo_categoria_prenda");

        builder.HasOne<HistorialContratoDb>()
            .WithMany()
            .HasForeignKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato })
            .HasConstraintName("FK_H_DETALLES_CONTRATOS_HISTORIAL_CONTRATOS1");
    }
}
