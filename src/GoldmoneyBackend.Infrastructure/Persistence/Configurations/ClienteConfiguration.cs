using GoldmoneyBackend.Domain.Entities.Clientes;
using GoldmoneyBackend.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("ClientesBackend");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Documento)
            .HasConversion(
                documento => documento.Value,
                value => Documento.Create(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        builder.Property(c => c.FechaActualizacion);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.HasIndex(c => c.Documento)
            .IsUnique();
    }
}