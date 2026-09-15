using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class GruposDataService : IGruposDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public GruposDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GrupoDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Grupos
            .AsNoTracking()
            .OrderBy(x => x.CodigoEmpresa)
            .ThenBy(x => x.CodigoGrupo)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<GrupoDbDto?> GetByKeyAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var empresa = codigoEmpresa.Trim();
        return await _dbContext.Grupos
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa && x.CodigoGrupo == codigoGrupo)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Infrastructure.Persistence.Legacy.Entities.GrupoDb, GrupoDbDto>> ToDto() =>
        x => new GrupoDbDto(
            x.CodigoEmpresa,
            x.CodigoGrupo,
            x.AbreviaturaGrupo,
            x.FechaCreacion,
            x.DescripcionGrupo,
            x.TasaInteres,
            x.MesesPlazo,
            x.EstatusSerie,
            x.SerieInicial,
            x.EstadoGrupo,
            x.CaracteristicaGrupo,
            x.BloquearInteres,
            x.BloquearPlazo,
            x.TasaInteresNocturna);
}
