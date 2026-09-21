using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class SecuenciaEmpresaDbConfiguration : IEntityTypeConfiguration<SecuenciaEmpresaDb>
{
    public void Configure(EntityTypeBuilder<SecuenciaEmpresaDb> builder)
    {
        builder.ToTable("SECUENCIAS_EMPRESA");
        builder.HasKey(x => new { x.CodigoEmpresa, x.CodigoSecuencia });
        builder.Property(x => x.CodigoEmpresa).HasColumnName("codigo_empresa").HasMaxLength(5).IsUnicode(false).IsRequired();
        builder.Property(x => x.CodigoSecuencia).HasColumnName("codigo_secuencia").HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(x => x.SecuenciaActual).HasColumnName("secuencia_actual");
    }
}