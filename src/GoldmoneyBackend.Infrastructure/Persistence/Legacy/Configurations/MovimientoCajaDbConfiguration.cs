using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class MovimientoCajaDbConfiguration : IEntityTypeConfiguration<MovimientoCajaDb>
{
    public void Configure(EntityTypeBuilder<MovimientoCajaDb> builder)
    {
        builder.ToTable("MOVIMIENTO_CAJA");
        builder.HasKey(x => new { x.CodigoEmpresa, x.NumeroMovimiento, x.CodigoTransaccion });

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.NumeroMovimiento).HasColumnName("numero_movimiento");
        builder.Property(x => x.CodigoTransaccion).HasColumnName("codigo_transaccion").HasMaxLength(2).IsUnicode(false).IsRequired();
        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.MontoTransaccion).HasColumnName("monto_transaccion").HasColumnType("money");
        builder.Property(x => x.FechaTransaccion).HasColumnName("fecha_transaccion");
        builder.Property(x => x.HoraTransaccion).HasColumnName("hora_transaccion");
        builder.Property(x => x.MotivoTransaccion).HasColumnName("motivo_transaccion").HasMaxLength(60).IsUnicode(false);
        builder.Property(x => x.UsuarioResponsable).HasColumnName("usuario_responsable").HasMaxLength(14).IsUnicode(false);

        builder.HasOne<EmpresaDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoEmpresa)
            .HasConstraintName("FK_MOVIMIENTO_CAJA_EMPRESA");
    }
}