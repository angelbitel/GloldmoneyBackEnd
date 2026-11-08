using GoldmoneyBackend.Infrastructure.Persistence.Cobros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class CobroConfiguration : IEntityTypeConfiguration<Cobro>
{
    public void Configure(EntityTypeBuilder<Cobro> builder)
    {
        builder.ToTable("Cobros");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Usuario).HasMaxLength(50).IsRequired();

        // Mismo tamaño que CLIENTES.id_cliente.
        builder.Property(x => x.IdCliente).HasMaxLength(14).IsUnicode(false).IsRequired();

        builder.Property(x => x.TotalMetodos).HasPrecision(18, 2);
        builder.Property(x => x.TotalDocumentos).HasPrecision(18, 2);

        builder.HasIndex(x => x.FechaUtc);
        builder.HasIndex(x => x.IdCliente);

        builder.HasMany(x => x.Metodos).WithOne().HasForeignKey(x => x.CobroId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Documentos).WithOne().HasForeignKey(x => x.CobroId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CobroMetodoPagoConfiguration : IEntityTypeConfiguration<CobroMetodoPago>
{
    public void Configure(EntityTypeBuilder<CobroMetodoPago> builder)
    {
        builder.ToTable("CobrosMetodoPago");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Metodo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Monto).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.CobroId, x.Metodo }).IsUnique();
    }
}

public sealed class CobroDocumentoConfiguration : IEntityTypeConfiguration<CobroDocumento>
{
    public void Configure(EntityTypeBuilder<CobroDocumento> builder)
    {
        builder.ToTable("CobrosDocumento");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tipo).HasMaxLength(10).IsUnicode(false).IsRequired();

        // Mismos tamaños que CONTRATOS.codigo_empresa y numero_contrato.
        builder.Property(x => x.CodigoEmpresa).HasMaxLength(2).IsUnicode(false).IsRequired();
        builder.Property(x => x.NumeroContrato).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Total).HasPrecision(18, 2);

        // Un recibo solo puede pertenecer a un cobro.
        builder.HasIndex(x => new { x.CodigoEmpresa, x.Tipo, x.NumeroRecibo }).IsUnique();
    }
}
