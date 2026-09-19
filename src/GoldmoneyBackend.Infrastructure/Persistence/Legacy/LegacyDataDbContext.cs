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
    public DbSet<IniciarCierreSesionDb> SesionesEmpresa => Set<IniciarCierreSesionDb>();
    public DbSet<ParametrosEmpresaDb> ParametrosEmpresa => Set<ParametrosEmpresaDb>();
    public DbSet<PaisDb> Paises => Set<PaisDb>();
    public DbSet<ProvinciaDb> Provincias => Set<ProvinciaDb>();
    public DbSet<DistritoDb> Distritos => Set<DistritoDb>();
    public DbSet<CorregimientoDb> Corregimientos => Set<CorregimientoDb>();
    public DbSet<DetalleContratoDb> DetallesContratos => Set<DetalleContratoDb>();
    public DbSet<ContratoDb> Contratos => Set<ContratoDb>();
    public DbSet<MovimientoCajaDb> MovimientosCaja => Set<MovimientoCajaDb>();
    public DbSet<MovimientoTemporalDb> MovimientosTemporales => Set<MovimientoTemporalDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClienteDbConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
