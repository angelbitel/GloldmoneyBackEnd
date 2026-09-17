using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class CorregimientoDbConfiguration : IEntityTypeConfiguration<CorregimientoDb>
{
    public void Configure(EntityTypeBuilder<CorregimientoDb> builder)
    {
        builder.ToTable("CORREGIMIENTO");
        builder.HasKey(x => x.CodigoCorregimiento);

        builder.Property(x => x.CodigoCorregimiento)
            .HasColumnName("codigo_corregimiento")
            .HasMaxLength(5)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CodigoDistrito)
            .HasColumnName("codigo_distrito")
            .HasMaxLength(5)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.NombreCorregimiento).HasColumnName("nombre_corregimiento").HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.Activo).HasColumnName("activo");

        builder.HasOne<DistritoDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoDistrito)
            .HasConstraintName("FK_CORREGIMIENTO_DISTRITO");
    }
}
