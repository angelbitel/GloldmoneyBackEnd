using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class EmpeniosReadService : IEmpeniosReadService
{
    private readonly LegacyDataDbContext _dbContext;

    public EmpeniosReadService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContratoDto?> GetContratoByIdAsync(string contratoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contratoId))
        {
            return null;
        }

        var id = contratoId.Trim();

        return await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => (x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim()) == id)
            .Select(x => new ContratoDto(
                x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim(),
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.IdCliente,
                x.FechaCreacion ?? DateTime.MinValue,
                x.CapitalPrestado ?? 0m,
                x.SaldoCapital ?? 0m,
                x.UsuarioResponsable))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContratoDto>> GetContratosByCedulaAsync(string cedula, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return Array.Empty<ContratoDto>();
        }

        return await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => x.IdCliente == cedula.Trim())
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new ContratoDto(
                x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim(),
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.IdCliente,
                x.FechaCreacion ?? DateTime.MinValue,
                x.CapitalPrestado ?? 0m,
                x.SaldoCapital ?? 0m,
                x.UsuarioResponsable))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContratoCompletoDto?> GetContratoCompletoByIdAsync(string contratoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contratoId))
        {
            return null;
        }

        var id = contratoId.Trim();

        var contrato = await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => (x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim()) == id)
            .Select(x => new
            {
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.IdCliente,
                x.FechaCreacion,
                x.CapitalPrestado,
                x.Interes,
                x.SaldoActual,
                x.InteresMensual,
                x.Observacion,
                x.UltimaFechaPago,
                x.SaldoCapital,
                x.FechaVencimiento,
                x.PlazoPago,
                x.UsuarioResponsable,
                x.MontoMaximo,
                x.Serie
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (contrato is null) return null;

        var detalles = await _dbContext.DetallesContratos
            .AsNoTracking()
            .Where(d => d.CodigoEmpresa == contrato.CodigoEmpresa
                && d.CodigoGrupo == contrato.CodigoGrupo
                && d.NumeroContrato == contrato.NumeroContrato)
            .OrderBy(d => d.SecuenciaContratos)
            .Join(_dbContext.CategoriasPrenda.AsNoTracking(),
                detalle => detalle.CodigoCategoriaPrenda,
                categoria => categoria.CodigoCategoriaPrenda,
                (detalle, categoria) => new ContratoDetalleDto(
                    detalle.SecuenciaContratos,
                    detalle.Descripcion,
                    detalle.Kilates,
                    detalle.Peso,
                    detalle.CantidadProducto,
                    detalle.CodigoCategoriaPrenda,
                    categoria.NombreCategoriaPrenda))
            .ToListAsync(cancellationToken);

        return new ContratoCompletoDto(
            contrato.CodigoEmpresa.Trim() + contrato.CodigoGrupo.ToString() + contrato.NumeroContrato.Trim(),
            contrato.CodigoEmpresa,
            contrato.CodigoGrupo,
            contrato.NumeroContrato,
            contrato.IdCliente,
            contrato.FechaCreacion ?? DateTime.MinValue,
            contrato.CapitalPrestado ?? 0m,
            contrato.Interes ?? 0m,
            contrato.SaldoActual ?? 0m,
            contrato.InteresMensual ?? 0m,
            contrato.Observacion,
            contrato.UltimaFechaPago ?? DateTime.MinValue,
            contrato.SaldoCapital ?? 0m,
            contrato.FechaVencimiento ?? DateTime.MinValue,
            contrato.PlazoPago ?? 0,
            contrato.UsuarioResponsable,
            contrato.MontoMaximo ?? 0m,
            contrato.Serie,
            detalles);
    }
}
