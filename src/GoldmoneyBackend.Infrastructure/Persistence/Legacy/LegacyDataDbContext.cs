using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy;

public sealed class LegacyDataDbContext : DbContext
{
    public LegacyDataDbContext(DbContextOptions<LegacyDataDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClienteDb> Clientes => Set<ClienteDb>();
    public DbSet<EmpresaDb> Empresas => Set<EmpresaDb>();
    public DbSet<UsuarioDb> Usuarios => Set<UsuarioDb>();
    public DbSet<GrupoDb> Grupos => Set<GrupoDb>();
    public DbSet<CategoriaPrendaDb> CategoriasPrenda => Set<CategoriaPrendaDb>();
    public DbSet<ValorDelOroDb> ValoresDelOro => Set<ValorDelOroDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClienteDbConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
