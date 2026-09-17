using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class PaisDbConfiguration : IEntityTypeConfiguration<PaisDb>
{
    public void Configure(EntityTypeBuilder<PaisDb> builder)
    {
        builder.ToTable("PAISES");
        builder.HasKey(x => x.CodigoPais);

        builder.Property(x => x.CodigoPais)
            .HasColumnName("codigo_pais")
            .HasMaxLength(5)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.NombrePais).HasColumnName("nombre_pais").HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.Activo).HasColumnName("activo");
    }
}
