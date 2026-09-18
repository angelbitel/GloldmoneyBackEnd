using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class ContratoDbConfiguration : IEntityTypeConfiguration<ContratoDb>
{
    public void Configure(EntityTypeBuilder<ContratoDb> builder)
    {
        builder.ToTable("CONTRATOS");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato });

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.IdCliente).HasColumnName("id_cliente").HasMaxLength(14).IsUnicode(false).IsRequired();
        builder.Property(x => x.Serie).HasColumnName("serie").HasMaxLength(10).IsUnicode(false);
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        builder.Property(x => x.CapitalPrestado).HasColumnName("capital_prestado").HasColumnType("money");
        builder.Property(x => x.Interes).HasColumnName("interes").HasColumnType("decimal(4,2)");
        builder.Property(x => x.SaldoActual).HasColumnName("saldo_actual").HasColumnType("money");
        builder.Property(x => x.InteresMensual).HasColumnName("interes_mensual").HasColumnType("money");
        builder.Property(x => x.Observacion).HasColumnName("observacion").HasMaxLength(80).IsUnicode(false);
        builder.Property(x => x.UltimaFechaPago).HasColumnName("ultima_fecha_pago");
        builder.Property(x => x.SaldoCapital).HasColumnName("saldo_capital").HasColumnType("money");
        builder.Property(x => x.FechaVencimiento).HasColumnName("fecha_vencimiento");
        builder.Property(x => x.PlazoPago).HasColumnName("plazo_pago");
        builder.Property(x => x.UsuarioResponsable).HasColumnName("usuario_responsable").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.HoraTransaccion).HasColumnName("hora_transaccion");
        builder.Property(x => x.MontoMaximo).HasColumnName("monto_maximo").HasColumnType("money");

        builder.HasOne<ClienteDb>()
            .WithMany()
            .HasForeignKey(x => x.IdCliente)
            .HasConstraintName("FK_CONTRATOS_CLIENTES");

        builder.HasOne<GrupoDb>()
            .WithMany()
            .HasForeignKey(x => new { x.CodigoEmpresa, x.CodigoGrupo })
            .HasConstraintName("FK_CONTRATOS_GRUPOS");
    }
}