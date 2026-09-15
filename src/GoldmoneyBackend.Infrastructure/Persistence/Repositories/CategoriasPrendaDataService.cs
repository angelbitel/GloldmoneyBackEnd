using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class CategoriasPrendaDataService : ICategoriasPrendaDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public CategoriasPrendaDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoriaPrendaDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.CategoriasPrenda
            .AsNoTracking()
            .OrderBy(x => x.CodigoCategoriaPrenda)
            .Select(x => new CategoriaPrendaDbDto(
                x.CodigoCategoriaPrenda,
                x.NombreCategoriaPrenda,
                x.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoriaPrendaDbDto?> GetByIdAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken)
    {
        return await _dbContext.CategoriasPrenda
            .AsNoTracking()
            .Where(x => x.CodigoCategoriaPrenda == codigoCategoriaPrenda)
            .Select(x => new CategoriaPrendaDbDto(
                x.CodigoCategoriaPrenda,
                x.NombreCategoriaPrenda,
                x.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
