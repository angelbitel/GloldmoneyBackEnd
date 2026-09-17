using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class DistritoDbConfiguration : IEntityTypeConfiguration<DistritoDb>
{
    public void Configure(EntityTypeBuilder<DistritoDb> builder)
    {
        builder.ToTable("DISTRITO");
        builder.HasKey(x => x.CodigoDistrito);

        builder.Property(x => x.CodigoDistrito)
            .HasColumnName("codigo_distrito")
            .HasMaxLength(5)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CodigoProvincia)
            .HasColumnName("codigo_provincia")
            .HasMaxLength(5)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.NombreDistrito).HasColumnName("nombre_distrito").HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.Activo).HasColumnName("activo");

        builder.HasOne<ProvinciaDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoProvincia)
            .HasConstraintName("FK_DISTRITO_PROVINCIA");
    }
}
