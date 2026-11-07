using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class RestablecimientoContrasenaConfiguration : IEntityTypeConfiguration<RestablecimientoContrasena>
{
    public void Configure(EntityTypeBuilder<RestablecimientoContrasena> builder)
    {
        builder.ToTable("RestablecimientosContrasena");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreUsuario)
            .HasMaxLength(15)
            .IsUnicode(false)
            .IsRequired();

        // SHA-256 en hexadecimal.
        builder.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.NombreUsuario, x.CreadoEnUtc });
    }
}
