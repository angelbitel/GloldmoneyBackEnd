using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class CategoriaPrendaDbConfiguration : IEntityTypeConfiguration<CategoriaPrendaDb>
{
    public void Configure(EntityTypeBuilder<CategoriaPrendaDb> builder)
    {
        builder.ToTable("CATEGORIAS_PRENDA");
        builder.HasKey(x => x.CodigoCategoriaPrenda);
        builder.Property(x => x.CodigoCategoriaPrenda).HasColumnName("codigo_categoria_prenda");
        builder.Property(x => x.NombreCategoriaPrenda).HasColumnName("nombre_categoria_prenda").HasMaxLength(50);
        builder.Property(x => x.Activo).HasColumnName("activo");
    }
}
