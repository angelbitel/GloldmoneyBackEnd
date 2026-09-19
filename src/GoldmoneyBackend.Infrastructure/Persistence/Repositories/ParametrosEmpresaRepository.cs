using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ParametrosEmpresaRepository : IParametrosEmpresaRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public ParametrosEmpresaRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ParametrosEmpresaDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .OrderBy(x => x.CodigoEmpresa)
            .Select(ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<ParametrosEmpresaDbDto?> GetByKeyAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        ValidateKey(codigoEmpresa);
        var empresa = codigoEmpresa.Trim();

        return await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa)
            .Select(ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(ParametrosEmpresaDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.CodigoEmpresa);
        var empresa = dto.CodigoEmpresa.Trim();

        var exists = await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .AnyAsync(x => x.CodigoEmpresa == empresa, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existen parametros para esa empresa en tabla PARAMETROS_EMPRESA.");
        }

        var entity = new ParametrosEmpresaDb { CodigoEmpresa = empresa };
        MapDtoToEntity(dto, entity);

        await _dbContext.ParametrosEmpresa.AddAsync(entity, cancellationToken);
        await SaveChangesAsync("crear", cancellationToken);
    }

    public async Task UpdateAsync(string codigoEmpresa, ParametrosEmpresaDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(codigoEmpresa);
        var entity = await _dbContext.ParametrosEmpresa
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Parametros de empresa no encontrados en tabla PARAMETROS_EMPRESA.");
        }

        MapDtoToEntity(dto, entity);
        await SaveChangesAsync("actualizar", cancellationToken);
    }

    public async Task DeleteAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        ValidateKey(codigoEmpresa);
        var entity = await _dbContext.ParametrosEmpresa
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim(), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Parametros de empresa no encontrados en tabla PARAMETROS_EMPRESA.");
        }

        _dbContext.ParametrosEmpresa.Remove(entity);
        await SaveChangesAsync("eliminar", cancellationToken);
    }

    private static void MapDtoToEntity(ParametrosEmpresaDbUpsertDto dto, ParametrosEmpresaDb entity)
    {
        entity.PermitirDescuento = dto.PermitirDescuento;
        entity.TrabajarKilates = dto.TrabajarKilates;
        entity.ExistenciaReloj = dto.ExistenciaReloj;
        entity.BloquearPlazoInt = dto.BloquearPlazoInt;
        entity.CodigoBarra = dto.CodigoBarra;
        entity.OperarAbonoParcial = dto.OperarAbonoParcial;
        entity.AbonarVencidos = dto.AbonarVencidos;
        entity.AbonarCapital = dto.AbonarCapital;
        entity.TipoCobroInteres = dto.TipoCobroInteres;
        entity.DiasGracia = dto.DiasGracia;
        entity.ImprimirEmpActivos = dto.ImprimirEmpActivos;
        entity.ImprimirCopia = dto.ImprimirCopia;
        entity.OperarEtiquetas = dto.OperarEtiquetas;
        entity.ControlarPuerto = dto.ControlarPuerto;
        entity.PuertoContratos = TrimOrNull(dto.PuertoContratos);
        entity.PuertoPagos = TrimOrNull(dto.PuertoPagos);
        entity.TipoImpRecibo = dto.TipoImpRecibo;
        entity.ControlarCapital = dto.ControlarCapital;
        entity.TipoControlCapital = dto.TipoControlCapital;
        entity.TipoValorCapital = dto.TipoValorCapital;
        entity.ContMontoCaja = dto.ContMontoCaja;
        entity.CostoCopia = dto.CostoCopia;
        entity.AnulacionControladaTiempo = dto.AnulacionControladaTiempo;
        entity.ImpEtiquetaCopia = dto.ImpEtiquetaCopia;
        entity.DetallePrestablecido = dto.DetallePrestablecido;
        entity.RestarAbonoCapital = dto.RestarAbonoCapital;
        entity.CapitalSinDecimal = dto.CapitalSinDecimal;
        entity.PermitirPagoAdelantado = dto.PermitirPagoAdelantado;
        entity.ImprimirReciboPago = dto.ImprimirReciboPago;
        entity.ModeloImpresoraCodBar = dto.ModeloImpresoraCodBar;
        entity.ControlarAnulacion = dto.ControlarAnulacion;
        entity.ControlarImpresionContratos = dto.ControlarImpresionContratos;
        entity.ControlarProcesoAnulacion = dto.ControlarProcesoAnulacion;
        entity.NoEtiquetasPagos = dto.NoEtiquetasPagos;
        entity.MontoMinContrato = dto.MontoMinContrato;
        entity.TipoEtiquetaPago = dto.TipoEtiquetaPago;
        entity.ImprimirEtiquetaRetiro = dto.ImprimirEtiquetaRetiro;
        entity.TiempoAnulacion = dto.TiempoAnulacion;
        entity.RepModuloSoporte = dto.RepModuloSoporte;
        entity.TipoMontoMaximo = dto.TipoMontoMaximo;
        entity.PorcentajeMontoMaximo = dto.PorcentajeMontoMaximo;
        entity.ManejoCierreAutomatico = dto.ManejoCierreAutomatico;
        entity.HoraCierreAutomatico = dto.HoraCierreAutomatico;
        entity.UltimoCierreAutomatico = dto.UltimoCierreAutomatico;
        entity.StatusManejoScaner = dto.StatusManejoScaner;
        entity.RutaDirImagenes = TrimOrNull(dto.RutaDirImagenes);
        entity.MaxLengthCharDescripcion = dto.MaxLengthCharDescripcion;
        entity.MostrarComentarioCliente = dto.MostrarComentarioCliente;
        entity.MostrarComentarioContrato = dto.MostrarComentarioContrato;
        entity.MostrarColumnaCantProducto = dto.MostrarColumnaCantProducto;
        entity.NoEtiquetasRetiros = dto.NoEtiquetasRetiros;
    }

    private static System.Linq.Expressions.Expression<Func<ParametrosEmpresaDb, ParametrosEmpresaDbDto>> ToDto() =>
        x => new ParametrosEmpresaDbDto(
            x.CodigoEmpresa,
            x.PermitirDescuento,
            x.TrabajarKilates,
            x.ExistenciaReloj,
            x.BloquearPlazoInt,
            x.CodigoBarra,
            x.OperarAbonoParcial,
            x.AbonarVencidos,
            x.AbonarCapital,
            x.TipoCobroInteres,
            x.DiasGracia,
            x.ImprimirEmpActivos,
            x.ImprimirCopia,
            x.OperarEtiquetas,
            x.ControlarPuerto,
            x.PuertoContratos,
            x.PuertoPagos,
            x.TipoImpRecibo,
            x.ControlarCapital,
            x.TipoControlCapital,
            x.TipoValorCapital,
            x.ContMontoCaja,
            x.CostoCopia,
            x.AnulacionControladaTiempo,
            x.ImpEtiquetaCopia,
            x.DetallePrestablecido,
            x.RestarAbonoCapital,
            x.CapitalSinDecimal,
            x.PermitirPagoAdelantado,
            x.ImprimirReciboPago,
            x.ModeloImpresoraCodBar,
            x.ControlarAnulacion,
            x.ControlarImpresionContratos,
            x.ControlarProcesoAnulacion,
            x.NoEtiquetasPagos,
            x.MontoMinContrato,
            x.TipoEtiquetaPago,
            x.ImprimirEtiquetaRetiro,
            x.TiempoAnulacion,
            x.RepModuloSoporte,
            x.TipoMontoMaximo,
            x.PorcentajeMontoMaximo,
            x.ManejoCierreAutomatico,
            x.HoraCierreAutomatico,
            x.UltimoCierreAutomatico,
            x.StatusManejoScaner,
            x.RutaDirImagenes,
            x.MaxLengthCharDescripcion,
            x.MostrarComentarioCliente,
            x.MostrarComentarioContrato,
            x.MostrarColumnaCantProducto,
            x.NoEtiquetasRetiros);

    private async Task SaveChangesAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo {operation} los parametros de empresa en tabla PARAMETROS_EMPRESA. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ValidateKey(string codigoEmpresa)
    {
        if (string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            throw new DomainValidationException("codigo_empresa es obligatorio.");
        }

        if (codigoEmpresa.Trim().Length > 2)
        {
            throw new DomainValidationException("codigo_empresa no puede exceder 2 caracteres.");
        }
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
