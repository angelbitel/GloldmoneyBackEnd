using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class HistorialContratoDbConfiguration : IEntityTypeConfiguration<HistorialContratoDb>
{
    public void Configure(EntityTypeBuilder<HistorialContratoDb> builder)
    {
        builder.ToTable("HISTORIAL_CONTRATOS");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato });

        builder.Property(x => x.CodigoEmpresa).HasColumnName("codigo_empresa").HasMaxLength(2).IsUnicode(false).IsRequired();
        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.StatusHistorial).HasColumnName("status_historial").HasMaxLength(2).IsUnicode(false).IsRequired();
        builder.Property(x => x.IdCliente).HasColumnName("id_cliente").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        builder.Property(x => x.FechaVencimiento).HasColumnName("fecha_vencimiento");
        builder.Property(x => x.CapitalPrestado).HasColumnName("capital_prestado").HasColumnType("decimal(9,2)");
        builder.Property(x => x.TasaInteres).HasColumnName("tasa_interes").HasColumnType("decimal(2,0)");
        builder.Property(x => x.InteresMensual).HasColumnName("interes_mensual").HasColumnType("decimal(6,2)");
        builder.Property(x => x.PlazoPago).HasColumnName("plazo_pago");
        builder.Property(x => x.UltimaFechaPago).HasColumnName("ultima_fecha_pago");
        builder.Property(x => x.Saldo).HasColumnName("saldo").HasColumnType("decimal(8,2)");
        builder.Property(x => x.SaldoCapital).HasColumnName("saldo_capital").HasColumnType("decimal(8,2)");
        builder.Property(x => x.UsuarioResponsable).HasColumnName("usuario_responsable").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.FechaIngreso).HasColumnName("fecha_ingreso");
        builder.Property(x => x.HoraIngreso).HasColumnName("hora_ingreso");
        builder.Property(x => x.UsuarioCreacion).HasColumnName("usuario_creacion").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.HoraCreacion).HasColumnName("hora_creacion");
        builder.Property(x => x.MontoMaximo).HasColumnName("monto_maximo").HasColumnType("money");
    }
}
