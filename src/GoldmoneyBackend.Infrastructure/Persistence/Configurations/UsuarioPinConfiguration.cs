using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class UsuarioPinConfiguration : IEntityTypeConfiguration<UsuarioPin>
{
    public void Configure(EntityTypeBuilder<UsuarioPin> builder)
    {
        builder.ToTable("UsuariosPin");

        builder.HasKey(x => x.NombreUsuario);

        // Mismo tamaño que USUARIOS.nombre_usuario.
        builder.Property(x => x.NombreUsuario)
            .HasMaxLength(15)
            .IsUnicode(false);

        builder.Property(x => x.PinHash)
            .HasMaxLength(256)
            .IsUnicode(false)
            .IsRequired();
    }
}
