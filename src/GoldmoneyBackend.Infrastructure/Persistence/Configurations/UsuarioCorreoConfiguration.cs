using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class UsuarioCorreoConfiguration : IEntityTypeConfiguration<UsuarioCorreo>
{
    public void Configure(EntityTypeBuilder<UsuarioCorreo> builder)
    {
        builder.ToTable("UsuariosCorreo");

        builder.HasKey(x => x.NombreUsuario);

        // Mismo tamaño que USUARIOS.nombre_usuario.
        builder.Property(x => x.NombreUsuario)
            .HasMaxLength(15)
            .IsUnicode(false);

        builder.Property(x => x.Correo)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => x.Correo).IsUnique();
    }
}
