using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ProvinciasRepository : IProvinciasRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public ProvinciasRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProvinciaDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Provincias
            .AsNoTracking()
            .OrderBy(x => x.CodigoProvincia)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<ProvinciaDbDto?> GetByKeyAsync(string codigoProvincia, CancellationToken cancellationToken)
    {
        ValidateKey(codigoProvincia);
        var codigo = codigoProvincia.Trim();

        return await _dbContext.Provincias
            .AsNoTracking()
            .Where(x => x.CodigoProvincia == codigo)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(ProvinciaDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoProvincia);
        var codigo = dto.CodigoProvincia.Trim();

        var exists = await _dbContext.Provincias
            .AsNoTracking()
            .AnyAsync(x => x.CodigoProvincia == codigo, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe una provincia con ese codigo_provincia.");
        }

        _dbContext.Provincias.Add(new ProvinciaDb
        {
            CodigoProvincia = codigo,
            NombreProvincia = TrimOrNull(dto.NombreProvincia),
            Activo = dto.Activo
        });

        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(string codigoProvincia, ProvinciaDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(codigoProvincia);
        var entity = await _dbContext.Provincias
            .FirstOrDefaultAsync(x => x.CodigoProvincia == codigoProvincia.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Provincia no encontrada en tabla PROVINCIA.");
        }

        entity.NombreProvincia = TrimOrNull(dto.NombreProvincia);
        entity.Activo = dto.Activo;

        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(string codigoProvincia, CancellationToken cancellationToken)
    {
        ValidateKey(codigoProvincia);
        var entity = await _dbContext.Provincias
            .FirstOrDefaultAsync(x => x.CodigoProvincia == codigoProvincia.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Provincia no encontrada en tabla PROVINCIA.");
        }

        _dbContext.Provincias.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<ProvinciaDb, ProvinciaDbDto>> ToDto() =>
        x => new ProvinciaDbDto(x.CodigoProvincia, x.NombreProvincia, x.Activo);

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} la provincia en tabla PROVINCIA. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateKey(string codigoProvincia)
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
