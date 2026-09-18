using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class ClienteDbConfiguration : IEntityTypeConfiguration<ClienteDb>
{
    public void Configure(EntityTypeBuilder<ClienteDb> builder)
    {
        builder.ToTable("CLIENTES");

        builder.HasKey(x => x.IdCliente);

        builder.Property(x => x.IdCliente)
            .HasColumnName("id_cliente")
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(x => x.Apellido).HasColumnName("apellido").HasMaxLength(20);
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(20);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(20);
        builder.Property(x => x.Estatus).HasColumnName("estatus");
        builder.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(40);
        builder.Property(x => x.Comentario).HasColumnName("comentario").HasMaxLength(300);
        builder.Property(x => x.CodigoPais).HasColumnName("codigo_pais").HasMaxLength(5);
        builder.Property(x => x.CodigoProvincia).HasColumnName("codigo_provincia").HasMaxLength(5);
        builder.Property(x => x.CodigoDistrito).HasColumnName("codigo_distrito").HasMaxLength(5);
        builder.Property(x => x.CodigoCorregimiento).HasColumnName("codigo_corregimiento").HasMaxLength(5);

        builder.HasOne<PaisDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoPais)
            .HasConstraintName("FK_CLIENTES_PAISES");

        builder.HasOne<ProvinciaDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoProvincia)
            .HasConstraintName("FK_CLIENTES_PROVINCIA");

        builder.HasOne<DistritoDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoDistrito)
            .HasConstraintName("FK_CLIENTES_DISTRITO");

        builder.HasOne<CorregimientoDb>()
            .WithMany()
            .HasForeignKey(x => x.CodigoCorregimiento)
            .HasConstraintName("FK_CLIENTES_CORREGIMIENTO");
    }
}
