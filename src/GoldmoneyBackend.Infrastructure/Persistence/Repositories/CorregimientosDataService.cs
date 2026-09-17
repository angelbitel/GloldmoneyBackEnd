using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class CorregimientosDataService : ICorregimientosDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public CorregimientosDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CorregimientoDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Corregimientos
            .AsNoTracking()
            .OrderBy(x => x.CodigoCorregimiento)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<CorregimientoDbDto?> GetByKeyAsync(string codigoCorregimiento, CancellationToken cancellationToken)
    {
        ValidateKey(codigoCorregimiento);
        var codigo = codigoCorregimiento.Trim();

        return await _dbContext.Corregimientos
            .AsNoTracking()
            .Where(x => x.CodigoCorregimiento == codigo)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(CorregimientoDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoCorregimiento);
        ValidateCodigoDistrito(dto.CodigoDistrito);
        var codigo = dto.CodigoCorregimiento.Trim();
        var codigoDistrito = dto.CodigoDistrito.Trim();

        var exists = await _dbContext.Corregimientos
            .AsNoTracking()
            .AnyAsync(x => x.CodigoCorregimiento == codigo, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un corregimiento con ese codigo_corregimiento.");
        }

        var distritoExiste = await _dbContext.Distritos
            .AsNoTracking()
            .AnyAsync(x => x.CodigoDistrito == codigoDistrito, cancellationToken);

        if (!distritoExiste)
        {
            throw new NotFoundDomainException("El distrito indicado no existe en tabla DISTRITO.");
        }

        _dbContext.Corregimientos.Add(new CorregimientoDb
        {
            CodigoCorregimiento = codigo,
            CodigoDistrito = codigoDistrito,
            NombreCorregimiento = TrimOrNull(dto.NombreCorregimiento),
            Activo = dto.Activo
        });

        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(string codigoCorregimiento, CorregimientoDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(codigoCorregimiento);
        ValidateCodigoDistrito(dto.CodigoDistrito);

        var entity = await _dbContext.Corregimientos
            .FirstOrDefaultAsync(x => x.CodigoCorregimiento == codigoCorregimiento.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Corregimiento no encontrado en tabla CORREGIMIENTO.");
        }

        var codigoDistrito = dto.CodigoDistrito.Trim();
        var distritoExiste = await _dbContext.Distritos
            .AsNoTracking()
            .AnyAsync(x => x.CodigoDistrito == codigoDistrito, cancellationToken);

        if (!distritoExiste)
        {
            throw new NotFoundDomainException("El distrito indicado no existe en tabla DISTRITO.");
        }

        entity.CodigoDistrito = codigoDistrito;
        entity.NombreCorregimiento = TrimOrNull(dto.NombreCorregimiento);
        entity.Activo = dto.Activo;

        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(string codigoCorregimiento, CancellationToken cancellationToken)
    {
        ValidateKey(codigoCorregimiento);
        var entity = await _dbContext.Corregimientos
            .FirstOrDefaultAsync(x => x.CodigoCorregimiento == codigoCorregimiento.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Corregimiento no encontrado en tabla CORREGIMIENTO.");
        }

        _dbContext.Corregimientos.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<CorregimientoDb, CorregimientoDbDto>> ToDto() =>
        x => new CorregimientoDbDto(x.CodigoCorregimiento, x.CodigoDistrito, x.NombreCorregimiento, x.Activo);

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} el corregimiento en tabla CORREGIMIENTO. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateKey(string codigoCorregimiento)
    {
        if (string.IsNullOrWhiteSpace(codigoCorregimiento))
        {
            throw new DomainValidationException("codigo_corregimiento es obligatorio.");
        }

        if (codigoCorregimiento.Trim().Length > 5)
        {
            throw new DomainValidationException("codigo_corregimiento no puede exceder 5 caracteres.");
        }
    }

    private static void ValidateCodigoDistrito(string codigoDistrito)
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

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
