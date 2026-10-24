using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class PagoDbConfiguration : IEntityTypeConfiguration<PagoDb>
{
    public void Configure(EntityTypeBuilder<PagoDb> builder)
    {
        builder.ToTable("PAGOS");
        builder.HasKey(x => new { x.CodigoEmpresa, x.NumeroPagos, x.CodigoGrupo, x.NumeroContrato });

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.NumeroPagos).HasColumnName("numero_pagos");
        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.FechaPago).HasColumnName("fecha_pago");
        builder.Property(x => x.CantidadPagos).HasColumnName("cantidad_pagos");
        builder.Property(x => x.PagoInteres).HasColumnName("pago_interes").HasColumnType("decimal(8,2)");
        builder.Property(x => x.PagoCapital).HasColumnName("pago_capital").HasColumnType("decimal(8,2)");
        builder.Property(x => x.SaldoCapital).HasColumnName("saldo_capital").HasColumnType("decimal(8,2)");
        builder.Property(x => x.SaldoInteres).HasColumnName("saldo_interes").HasColumnType("decimal(8,2)");
        builder.Property(x => x.UltimaFechaPago).HasColumnName("ultima_fecha_pago");
        builder.Property(x => x.UsuarioResponsable).HasColumnName("usuario_responsable").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.SecuenciaRecibo).HasColumnName("secuencia_recibo");
        builder.Property(x => x.HoraTransaccion).HasColumnName("hora_transaccion");

        builder.HasOne<ContratoDb>()
            .WithMany()
            .HasForeignKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato })
            .HasConstraintName("FK_PAGOS_CONTRATOS");
    }
}