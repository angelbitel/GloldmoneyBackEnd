using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class IniciarCierreSesionDbConfiguration : IEntityTypeConfiguration<IniciarCierreSesionDb>
{
    public void Configure(EntityTypeBuilder<IniciarCierreSesionDb> builder)
    {
        builder.ToTable("INICIO_CIERRE_SESION");
        builder.HasKey(x => new { x.CodigoEmpresa, x.FechaApertura });

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.FechaApertura).HasColumnName("fecha_apertura");
        builder.Property(x => x.HoraCierre).HasColumnName("hora_cierre");
        builder.Property(x => x.UsuarioApertura).HasColumnName("usuario_apertura").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.UsuarioCierre).HasColumnName("usuario_cierre").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.ValorInicialCaja).HasColumnName("valor_inicial_caja").HasColumnType("decimal(8,2)");
        builder.Property(x => x.ValorFinalCaja).HasColumnName("valor_final_caja").HasColumnType("decimal(8,2)");
        builder.Property(x => x.StatusSesion).HasColumnName("status_sesion").HasColumnType("decimal(1,0)");
    }
}
