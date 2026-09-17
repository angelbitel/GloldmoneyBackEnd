using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class ProvinciaDbConfiguration : IEntityTypeConfiguration<ProvinciaDb>
{
    public void Configure(EntityTypeBuilder<ProvinciaDb> builder)
    {
        builder.ToTable("PROVINCIA");
        builder.HasKey(x => x.CodigoProvincia);

        builder.Property(x => x.CodigoProvincia)
            .HasColumnName("codigo_provincia")
            .HasMaxLength(5)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.NombreProvincia).HasColumnName("nombre_provincia").HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.Activo).HasColumnName("activo");
    }
}
