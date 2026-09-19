using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ValorDelOroRepository : IValorDelOroRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public ValorDelOroRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ValorDelOroDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ValoresDelOro
            .AsNoTracking()
            .OrderBy(x => x.CodigoEmpresa)
            .ThenBy(x => x.StatusCalidad)
            .ThenBy(x => x.Kilataje)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<ValorDelOroDbDto?> GetByKeyAsync(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        CancellationToken cancellationToken)
    {
        ValidateKey(codigoEmpresa, statusCalidad);
        var empresa = codigoEmpresa.Trim();
        var calidad = statusCalidad.Trim();

        return await _dbContext.ValoresDelOro
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa
                && x.StatusCalidad == calidad
                && x.Kilataje == kilataje)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(ValorDelOroDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoEmpresa, dto.StatusCalidad);
        var empresa = dto.CodigoEmpresa.Trim();
        var calidad = dto.StatusCalidad.Trim();

        var exists = await _dbContext.ValoresDelOro
            .AsNoTracking()
            .AnyAsync(x => x.CodigoEmpresa == empresa
                && x.StatusCalidad == calidad
                && x.Kilataje == dto.Kilataje, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un valor del oro con esa clave.");
        }

        _dbContext.ValoresDelOro.Add(new ValorDelOroDb
        {
            CodigoEmpresa = empresa,
            StatusCalidad = calidad,
            Kilataje = dto.Kilataje,
            MaximoValor = dto.MaximoValor,
            MinimoValor = dto.MinimoValor
        });

        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        ValorDelOroDbUpsertDto dto,
        CancellationToken cancellationToken)
    {
        ValidateKey(codigoEmpresa, statusCalidad);
        var entity = await _dbContext.ValoresDelOro
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim()
                && x.StatusCalidad == statusCalidad.Trim()
                && x.Kilataje == kilataje, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Valor del oro no encontrado en tabla VALOR_DEL_ORO.");
        }

        entity.MaximoValor = dto.MaximoValor;
        entity.MinimoValor = dto.MinimoValor;

        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        CancellationToken cancellationToken)
    {
        ValidateKey(codigoEmpresa, statusCalidad);
        var entity = await _dbContext.ValoresDelOro
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim()
                && x.StatusCalidad == statusCalidad.Trim()
                && x.Kilataje == kilataje, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Valor del oro no encontrado en tabla VALOR_DEL_ORO.");
        }

        _dbContext.ValoresDelOro.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<ValorDelOroDb, ValorDelOroDbDto>> ToDto() =>
        x => new ValorDelOroDbDto(
            x.CodigoEmpresa,
            x.StatusCalidad,
            x.Kilataje,
            x.MaximoValor,
            x.MinimoValor);

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} el valor del oro en tabla VALOR_DEL_ORO. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateKey(string codigoEmpresa, string statusCalidad)
    {
        if (string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            throw new DomainValidationException("codigo_empresa es obligatorio.");
        }

        if (codigoEmpresa.Trim().Length > 2)
        {
            throw new DomainValidationException("codigo_empresa no puede exceder 2 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(statusCalidad))
        {
            throw new DomainValidationException("status_calidad es obligatorio.");
        }

        if (statusCalidad.Trim().Length > 1)
        {
            throw new DomainValidationException("status_calidad no puede exceder 1 caracter.");
        }
    }
}
