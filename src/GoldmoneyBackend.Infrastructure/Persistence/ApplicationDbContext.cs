using GoldmoneyBackend.Domain.Entities.Clientes;
using GoldmoneyBackend.Infrastructure.Persistence.Cobros;
using GoldmoneyBackend.Infrastructure.Persistence.Configurations;
using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using GoldmoneyBackend.Infrastructure.Persistence.Soporte;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<UsuarioCorreo> UsuariosCorreo => Set<UsuarioCorreo>();
    public DbSet<CredencialUsuario> UsuariosCredencial => Set<CredencialUsuario>();
    public DbSet<UsuarioPin> UsuariosPin => Set<UsuarioPin>();
    public DbSet<RestablecimientoContrasena> RestablecimientosContrasena => Set<RestablecimientoContrasena>();
    public DbSet<Cobro> Cobros => Set<Cobro>();
    public DbSet<CobroDocumento> CobrosDocumento => Set<CobroDocumento>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMensaje> TicketMensajes => Set<TicketMensaje>();
    public DbSet<TicketAdjunto> TicketAdjuntos => Set<TicketAdjunto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Solo las configuraciones del modelo moderno: las de Persistence.Legacy mapean
        // tablas existentes del VB6 y no deben entrar en las migraciones de este contexto.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => type.Namespace == typeof(ClienteConfiguration).Namespace);
        base.OnModelCreating(modelBuilder);
    }
}
