using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class ParametrosEmpresaDbConfiguration : IEntityTypeConfiguration<ParametrosEmpresaDb>
{
    public void Configure(EntityTypeBuilder<ParametrosEmpresaDb> builder)
    {
        builder.ToTable("PARAMETROS_EMPRESA");
        builder.HasKey(x => x.CodigoEmpresa);

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.ContMontoCaja).HasColumnName("cont_monto_caja").HasColumnType("decimal(1,0)");
    }
}
