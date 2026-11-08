using GoldmoneyBackend.Domain.Entities.Clientes;
using GoldmoneyBackend.Infrastructure.Persistence.Cobros;
using GoldmoneyBackend.Infrastructure.Persistence.Configurations;
using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
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
    public DbSet<RestablecimientoContrasena> RestablecimientosContrasena => Set<RestablecimientoContrasena>();
    public DbSet<Cobro> Cobros => Set<Cobro>();
    public DbSet<CobroDocumento> CobrosDocumento => Set<CobroDocumento>();

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
