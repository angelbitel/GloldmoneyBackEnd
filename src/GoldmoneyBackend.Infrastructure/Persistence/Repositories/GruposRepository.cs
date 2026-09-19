using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class GruposRepository : IGruposRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public GruposRepository(LegacyDataDbContext dbContext)
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

    public async Task CreateAsync(GrupoDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateCodigoEmpresa(dto.CodigoEmpresa);
        var codigoEmpresa = dto.CodigoEmpresa.Trim();

        var exists = await _dbContext.Grupos
            .AsNoTracking()
            .AnyAsync(x => x.CodigoEmpresa == codigoEmpresa && x.CodigoGrupo == dto.CodigoGrupo, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un grupo con esa clave.");
        }

        _dbContext.Grupos.Add(ToEntity(dto, codigoEmpresa));
        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(string codigoEmpresa, int codigoGrupo, GrupoDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateCodigoEmpresa(codigoEmpresa);
        var empresa = codigoEmpresa.Trim();
        var entity = await _dbContext.Grupos
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == empresa && x.CodigoGrupo == codigoGrupo, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Grupo no encontrado en tabla GRUPOS.");
        }

        entity.AbreviaturaGrupo = TrimOrNull(dto.AbreviaturaGrupo);
        entity.FechaCreacion = dto.FechaCreacion;
        entity.DescripcionGrupo = TrimOrNull(dto.DescripcionGrupo);
        entity.TasaInteres = dto.TasaInteres;
        entity.MesesPlazo = dto.MesesPlazo;
        entity.EstatusSerie = dto.EstatusSerie;
        entity.SerieInicial = dto.SerieInicial;
        entity.EstadoGrupo = dto.EstadoGrupo;
        entity.CaracteristicaGrupo = dto.CaracteristicaGrupo;
        entity.BloquearInteres = dto.BloquearInteres;
        entity.BloquearPlazo = dto.BloquearPlazo;
        entity.TasaInteresNocturna = dto.TasaInteresNocturna;

        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        ValidateCodigoEmpresa(codigoEmpresa);
        var entity = await _dbContext.Grupos
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim() && x.CodigoGrupo == codigoGrupo, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Grupo no encontrado en tabla GRUPOS.");
        }

        _dbContext.Grupos.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
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

    private static GrupoDb ToEntity(GrupoDbUpsertDto dto, string codigoEmpresa) => new()
    {
        CodigoEmpresa = codigoEmpresa,
        CodigoGrupo = dto.CodigoGrupo,
        AbreviaturaGrupo = TrimOrNull(dto.AbreviaturaGrupo),
        FechaCreacion = dto.FechaCreacion,
        DescripcionGrupo = TrimOrNull(dto.DescripcionGrupo),
        TasaInteres = dto.TasaInteres,
        MesesPlazo = dto.MesesPlazo,
        EstatusSerie = dto.EstatusSerie,
        SerieInicial = dto.SerieInicial,
        EstadoGrupo = dto.EstadoGrupo,
        CaracteristicaGrupo = dto.CaracteristicaGrupo,
        BloquearInteres = dto.BloquearInteres,
        BloquearPlazo = dto.BloquearPlazo,
        TasaInteresNocturna = dto.TasaInteresNocturna
    };

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} el grupo en tabla GRUPOS. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateCodigoEmpresa(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("codigo_empresa es obligatorio.");
        }

        if (value.Trim().Length > 2)
        {
            throw new DomainValidationException("codigo_empresa no puede exceder 2 caracteres.");
        }
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
