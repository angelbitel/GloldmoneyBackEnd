using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class CopiaReciboDbConfiguration : IEntityTypeConfiguration<CopiaReciboDb>
{
    public void Configure(EntityTypeBuilder<CopiaReciboDb> builder)
    {
        builder.ToTable("COPIA_RECIBO");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato, x.NumeroCopia });

        builder.Property(x => x.CodigoEmpresa).HasColumnName("codigo_empresa").HasMaxLength(2).IsUnicode(false).IsRequired();
        builder.Property(x => x.NumeroCopia).HasColumnName("numero_copia");
        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.NumeroContrato).HasColumnName("numero_contrato").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.IdCliente).HasColumnName("id_cliente").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.Fecha).HasColumnName("fecha");
        builder.Property(x => x.Hora).HasColumnName("hora");
        builder.Property(x => x.Capital).HasColumnName("capital").HasColumnType("decimal(8,2)");
        builder.Property(x => x.UsuarioResponsable).HasColumnName("usuario_responsable").HasMaxLength(14).IsUnicode(false);
        builder.Property(x => x.SecuenciaRecibo).HasColumnName("secuencia_recibo");

        builder.HasOne<ContratoDb>()
            .WithMany()
            .HasForeignKey(x => new { x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato })
            .HasConstraintName("FK_COPIA_RECIBO_CONTRATOS")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
