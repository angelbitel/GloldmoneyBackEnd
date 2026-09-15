using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class GrupoDbConfiguration : IEntityTypeConfiguration<GrupoDb>
{
    public void Configure(EntityTypeBuilder<GrupoDb> builder)
    {
        builder.ToTable("GRUPOS");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoGrupo });
        builder.Property(x => x.CodigoEmpresa).HasColumnName("codigo_empresa").HasMaxLength(2).IsRequired();
        builder.Property(x => x.CodigoGrupo).HasColumnName("codigo_grupo");
        builder.Property(x => x.AbreviaturaGrupo).HasColumnName("abreviatura_grupo").HasMaxLength(10);
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        builder.Property(x => x.DescripcionGrupo).HasColumnName("descripcion_grupo").HasMaxLength(80);
        builder.Property(x => x.TasaInteres).HasColumnName("tasa_interes").HasPrecision(2, 0);
        builder.Property(x => x.MesesPlazo).HasColumnName("meses_plazo").HasPrecision(2, 0);
        builder.Property(x => x.EstatusSerie).HasColumnName("estatus_serie");
        builder.Property(x => x.SerieInicial).HasColumnName("serie_inicial").HasPrecision(10, 0);
        builder.Property(x => x.EstadoGrupo).HasColumnName("estado_grupo");
        builder.Property(x => x.CaracteristicaGrupo).HasColumnName("caracteristica_grupo");
        builder.Property(x => x.BloquearInteres).HasColumnName("bloquear_interes");
        builder.Property(x => x.BloquearPlazo).HasColumnName("bloquear_plazo");
        builder.Property(x => x.TasaInteresNocturna).HasColumnName("tasa_interes_nocturna").HasPrecision(2, 0);
    }
}
