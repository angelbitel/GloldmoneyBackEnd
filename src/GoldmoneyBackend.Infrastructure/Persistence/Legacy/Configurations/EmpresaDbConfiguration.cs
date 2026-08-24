using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class EmpresaDbConfiguration : IEntityTypeConfiguration<EmpresaDb>
{
    public void Configure(EntityTypeBuilder<EmpresaDb> builder)
    {
        builder.ToTable("EMPRESA");

        builder.HasKey(x => x.CodigoEmpresa);

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.NombreEmpresa).HasColumnName("nombre_empresa").HasMaxLength(50);
        builder.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(30);
        builder.Property(x => x.Ruc).HasColumnName("ruc").HasMaxLength(30);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(8);
        builder.Property(x => x.MontoInicial).HasColumnName("monto_inicial").HasColumnType("decimal(18,2)");
        builder.Property(x => x.MontoAuxiliar).HasColumnName("monto_auxiliar").HasColumnType("decimal(18,2)");
        builder.Property(x => x.ManejoCajaDep).HasColumnName("manejo_caja_dep");
        builder.Property(x => x.CodEmpresaCaja).HasColumnName("cod_empresa_caja");
    }
}
