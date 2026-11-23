using GoldmoneyBackend.Infrastructure.Persistence.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodigoEmpresa).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(x => x.Asunto).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Categoria).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(x => x.Prioridad).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Estado).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Origen).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Usuario).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreadoPor).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UsuarioAsignado).HasMaxLength(30);
        builder.Property(x => x.AsignadoA).HasMaxLength(100);
        builder.Property(x => x.UltimaRespuestaDe).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(x => x.NumeroContrato).HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.Pantalla).HasMaxLength(100);

        builder.HasMany(x => x.Mensajes)
            .WithOne()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CodigoEmpresa, x.FechaActualizacionUtc });
        builder.HasIndex(x => new { x.Estado, x.FechaActualizacionUtc });
    }
}

public sealed class TicketMensajeConfiguration : IEntityTypeConfiguration<TicketMensaje>
{
    public void Configure(EntityTypeBuilder<TicketMensaje> builder)
    {
        builder.ToTable("TicketMensajes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Usuario).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Autor).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Mensaje).HasMaxLength(8000).IsRequired();

        builder.HasMany(x => x.Adjuntos)
            .WithOne()
            .HasForeignKey(x => x.MensajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TicketAdjuntoConfiguration : IEntityTypeConfiguration<TicketAdjunto>
{
    public void Configure(EntityTypeBuilder<TicketAdjunto> builder)
    {
        builder.ToTable("TicketAdjuntos");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.Contenido).IsRequired();
    }
}
