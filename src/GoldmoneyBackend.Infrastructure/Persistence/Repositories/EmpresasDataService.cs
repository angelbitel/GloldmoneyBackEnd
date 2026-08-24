using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class EmpresasDataService : IEmpresasDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public EmpresasDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoEmpresa, "codigo_empresa");

        var exists = await _dbContext.Empresas
            .AsNoTracking()
            .AnyAsync(x => x.CodigoEmpresa == dto.CodigoEmpresa, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe una empresa con ese codigo_empresa.");
        }

        var entity = new EmpresaDb
        {
            CodigoEmpresa = dto.CodigoEmpresa.Trim(),
            NombreEmpresa = TrimOrNull(dto.NombreEmpresa),
            Direccion = TrimOrNull(dto.Direccion),
            Ruc = TrimOrNull(dto.Ruc),
            Telefono = TrimOrNull(dto.Telefono),
            MontoInicial = dto.MontoInicial,
            MontoAuxiliar = dto.MontoAuxiliar,
            ManejoCajaDep = dto.ManejoCajaDep,
            CodEmpresaCaja = dto.CodEmpresaCaja
        };

        await _dbContext.Empresas.AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo crear la empresa en tabla EMPRESA. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task UpdateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoEmpresa, "codigo_empresa");

        var entity = await _dbContext.Empresas
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == dto.CodigoEmpresa, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Empresa no encontrada en tabla EMPRESA.");
        }

        entity.NombreEmpresa = TrimOrNull(dto.NombreEmpresa);
        entity.Direccion = TrimOrNull(dto.Direccion);
        entity.Ruc = TrimOrNull(dto.Ruc);
        entity.Telefono = TrimOrNull(dto.Telefono);
        entity.MontoInicial = dto.MontoInicial;
        entity.MontoAuxiliar = dto.MontoAuxiliar;
        entity.ManejoCajaDep = dto.ManejoCajaDep;
        entity.CodEmpresaCaja = dto.CodEmpresaCaja;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo actualizar la empresa en tabla EMPRESA. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void ValidateKey(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} es obligatorio.");
        }
    }
}
