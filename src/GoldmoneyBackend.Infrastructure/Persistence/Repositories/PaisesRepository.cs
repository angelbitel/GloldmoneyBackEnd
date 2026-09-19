using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class PaisesRepository : IPaisesRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public PaisesRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PaisDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Paises
            .AsNoTracking()
            .OrderBy(x => x.CodigoPais)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<PaisDbDto?> GetByKeyAsync(string codigoPais, CancellationToken cancellationToken)
    {
        ValidateKey(codigoPais);
        var codigo = codigoPais.Trim();

        return await _dbContext.Paises
            .AsNoTracking()
            .Where(x => x.CodigoPais == codigo)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(PaisDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoPais);
        var codigo = dto.CodigoPais.Trim();

        var exists = await _dbContext.Paises
            .AsNoTracking()
            .AnyAsync(x => x.CodigoPais == codigo, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un pais con ese codigo_pais.");
        }

        _dbContext.Paises.Add(new PaisDb
        {
            CodigoPais = codigo,
            NombrePais = TrimOrNull(dto.NombrePais),
            Activo = dto.Activo
        });

        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(string codigoPais, PaisDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(codigoPais);
        var entity = await _dbContext.Paises
            .FirstOrDefaultAsync(x => x.CodigoPais == codigoPais.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Pais no encontrado en tabla PAISES.");
        }

        entity.NombrePais = TrimOrNull(dto.NombrePais);
        entity.Activo = dto.Activo;

        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(string codigoPais, CancellationToken cancellationToken)
    {
        ValidateKey(codigoPais);
        var entity = await _dbContext.Paises
            .FirstOrDefaultAsync(x => x.CodigoPais == codigoPais.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Pais no encontrado en tabla PAISES.");
        }

        _dbContext.Paises.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<PaisDb, PaisDbDto>> ToDto() =>
        x => new PaisDbDto(x.CodigoPais, x.NombrePais, x.Activo);

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} el pais en tabla PAISES. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateKey(string codigoPais)
    {
        if (string.IsNullOrWhiteSpace(codigoPais))
        {
            throw new DomainValidationException("codigo_pais es obligatorio.");
        }

        if (codigoPais.Trim().Length > 5)
        {
            throw new DomainValidationException("codigo_pais no puede exceder 5 caracteres.");
        }
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
