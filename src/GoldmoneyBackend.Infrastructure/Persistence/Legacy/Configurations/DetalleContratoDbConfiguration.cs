using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class DetalleContratoDbConfiguration : IEntityTypeConfiguration<DetalleContratoDb>
{
    public void Configure(EntityTypeBuilder<DetalleContratoDb> builder)
    {
        builder.ToTable("DETALLES_CONTRATOS");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato, x.SecuenciaContratos });

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.SecuenciaContratos).HasColumnName("secuencia_contratos");
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300).IsUnicode(false);
        builder.Property(x => x.Kilates).HasColumnName("kilates").HasColumnType("decimal(2,0)");
        builder.Property(x => x.Peso).HasColumnName("peso").HasColumnType("decimal(6,2)");
        builder.Property(x => x.CodigoReloj).HasColumnName("codigo_reloj");
        builder.Property(x => x.CantidadProducto).HasColumnName("cantidad_producto");
        builder.Property(x => x.CodigoCategoriaPrenda).HasColumnName("codigo_categoria_prenda");

        builder.HasOne<ContratoDb>()
            .WithMany()
            .HasForeignKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato })
            .HasConstraintName("FK_DETALLES_CONTRATOS_CONTRATOS");

        builder.HasOne<CategoriaPrendaDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoCategoriaPrenda)
            .HasConstraintName("FK_DETALLES_CONTRATOS_CATEGORIAS_PRENDA");
    }
}