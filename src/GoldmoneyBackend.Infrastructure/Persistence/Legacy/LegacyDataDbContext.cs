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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClienteDbConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
