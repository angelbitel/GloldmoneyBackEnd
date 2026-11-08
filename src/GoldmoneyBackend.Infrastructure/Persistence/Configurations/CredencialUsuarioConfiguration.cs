using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class CredencialUsuarioConfiguration : IEntityTypeConfiguration<CredencialUsuario>
{
    public void Configure(EntityTypeBuilder<CredencialUsuario> builder)
    {
        builder.ToTable("UsuariosCredencial");

        builder.HasKey(x => x.NombreUsuario);

        // Mismo tamaño que USUARIOS.nombre_usuario.
        builder.Property(x => x.NombreUsuario)
            .HasMaxLength(15)
            .IsUnicode(false);

        builder.Property(x => x.Hash)
            .HasMaxLength(256)
            .IsUnicode(false)
            .IsRequired();
    }
}
