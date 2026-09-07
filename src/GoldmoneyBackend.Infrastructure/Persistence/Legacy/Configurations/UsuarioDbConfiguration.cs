using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class UsuarioDbConfiguration : IEntityTypeConfiguration<UsuarioDb>
{
    public void Configure(EntityTypeBuilder<UsuarioDb> builder)
    {
        builder.ToTable("USUARIOS");

        builder.HasKey(x => new { x.Modulo, x.NombreUsuario });

        builder.Property(x => x.Modulo)
            .HasColumnName("modulo")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.NombreUsuario)
            .HasColumnName("nombre_usuario")
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(x => x.NombreCompleto)
            .HasColumnName("nombre_completo")
            .HasMaxLength(40);

        builder.Property(x => x.Contrasena)
            .HasColumnName("contrasena")
            .HasMaxLength(25)
            .IsRequired();

        builder.Property(x => x.StatusCuenta)
            .HasColumnName("status_cuenta")
            .HasColumnType("decimal(18,2)");
        builder.Property(x => x.IniciarDia).HasColumnName("iniciar_dia");
        builder.Property(x => x.AplicarDescuento).HasColumnName("aplicar_descuento");
        builder.Property(x => x.UsuarioAdmin).HasColumnName("usuario_admin");
        builder.Property(x => x.DatosRetroactivos).HasColumnName("datos_retroactivos");
        builder.Property(x => x.EfectuarAnulacion).HasColumnName("efectuar_anulacion");
        builder.Property(x => x.AccesoCashDrawer).HasColumnName("acceso_cash_drawer");
        builder.Property(x => x.UsuarioSoporte).HasColumnName("usuario_soporte");
    }
}
