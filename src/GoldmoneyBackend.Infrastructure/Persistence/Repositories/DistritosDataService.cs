using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class DistritosDataService : IDistritosDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public DistritosDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DistritoDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Distritos
            .AsNoTracking()
            .OrderBy(x => x.CodigoDistrito)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<DistritoDbDto?> GetByKeyAsync(string codigoDistrito, CancellationToken cancellationToken)
    {
        ValidateKey(codigoDistrito);
        var codigo = codigoDistrito.Trim();

        return await _dbContext.Distritos
            .AsNoTracking()
            .Where(x => x.CodigoDistrito == codigo)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(DistritoDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoDistrito);
        ValidateCodigoProvincia(dto.CodigoProvincia);
        var codigo = dto.CodigoDistrito.Trim();
        var codigoProvincia = dto.CodigoProvincia.Trim();

        var exists = await _dbContext.Distritos
            .AsNoTracking()
            .AnyAsync(x => x.CodigoDistrito == codigo, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un distrito con ese codigo_distrito.");
        }

        var provinciaExiste = await _dbContext.Provincias
            .AsNoTracking()
            .AnyAsync(x => x.CodigoProvincia == codigoProvincia, cancellationToken);

        if (!provinciaExiste)
        {
            throw new NotFoundDomainException("La provincia indicada no existe en tabla PROVINCIA.");
        }

        _dbContext.Distritos.Add(new DistritoDb
        {
            CodigoDistrito = codigo,
            CodigoProvincia = codigoProvincia,
            NombreDistrito = TrimOrNull(dto.NombreDistrito),
            Activo = dto.Activo
        });

        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(string codigoDistrito, DistritoDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(codigoDistrito);
        ValidateCodigoProvincia(dto.CodigoProvincia);

        var entity = await _dbContext.Distritos
            .FirstOrDefaultAsync(x => x.CodigoDistrito == codigoDistrito.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Distrito no encontrado en tabla DISTRITO.");
        }

        var codigoProvincia = dto.CodigoProvincia.Trim();
        var provinciaExiste = await _dbContext.Provincias
            .AsNoTracking()
            .AnyAsync(x => x.CodigoProvincia == codigoProvincia, cancellationToken);

        if (!provinciaExiste)
        {
            throw new NotFoundDomainException("La provincia indicada no existe en tabla PROVINCIA.");
        }

        entity.CodigoProvincia = codigoProvincia;
        entity.NombreDistrito = TrimOrNull(dto.NombreDistrito);
        entity.Activo = dto.Activo;

        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(string codigoDistrito, CancellationToken cancellationToken)
    {
        ValidateKey(codigoDistrito);
        var entity = await _dbContext.Distritos
            .FirstOrDefaultAsync(x => x.CodigoDistrito == codigoDistrito.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Distrito no encontrado en tabla DISTRITO.");
        }

        _dbContext.Distritos.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<DistritoDb, DistritoDbDto>> ToDto() =>
        x => new DistritoDbDto(x.CodigoDistrito, x.CodigoProvincia, x.NombreDistrito, x.Activo);

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} el distrito en tabla DISTRITO. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateKey(string codigoDistrito)
    {
        if (string.IsNullOrWhiteSpace(codigoDistrito))
        {
            throw new DomainValidationException("codigo_distrito es obligatorio.");
        }

        if (codigoDistrito.Trim().Length > 5)
        {
            throw new DomainValidationException("codigo_distrito no puede exceder 5 caracteres.");
        }
    }

    private static void ValidateCodigoProvincia(string codigoProvincia)
    {
        if (string.IsNullOrWhiteSpace(codigoProvincia))
        {
            throw new DomainValidationException("codigo_provincia es obligatorio.");
        }

        if (codigoProvincia.Trim().Length > 5)
        {
            throw new DomainValidationException("codigo_provincia no puede exceder 5 caracteres.");
        }
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
