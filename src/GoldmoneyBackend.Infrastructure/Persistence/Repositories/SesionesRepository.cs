using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class SesionesRepository : ISesionesRepository
{
    // PARAMETROS_EMPRESA.cont_monto_caja: 1 = usar valor del dia anterior, 0 = usar monto inicial.
    private const decimal ManejoCajaInicioDia = 0m;
    private const decimal ManejoCajaDiaAnterior = 1m;

    private readonly LegacyDataDbContext _dbContext;

    public SesionesRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EstadoSesionEmpresaDto>> GetEstadoSesionesAsync(CancellationToken cancellationToken)
    {
        var empresas = await _dbContext.Empresas
            .AsNoTracking()
            .Where(x => x.ManejoCajaDep == 0)
            .OrderBy(x => x.NombreEmpresa)
            .ToListAsync(cancellationToken);

        var resultado = new List<EstadoSesionEmpresaDto>(empresas.Count);

        foreach (var empresa in empresas)
        {
            var ultimaSesionAbierta = await _dbContext.SesionesEmpresa
                .AsNoTracking()
                .Where(x => x.CodigoEmpresa == empresa.CodigoEmpresa && x.StatusSesion == 1)
                .OrderByDescending(x => x.FechaApertura)
                .FirstOrDefaultAsync(cancellationToken);

            resultado.Add(new EstadoSesionEmpresaDto(
                empresa.CodigoEmpresa,
                empresa.NombreEmpresa,
                ultimaSesionAbierta is not null,
                ultimaSesionAbierta?.FechaApertura));
        }

        return resultado;
    }

    public async Task<SesionAbiertaDto> AbrirSesionAsync(AbrirSesionDto dto, CancellationToken cancellationToken)
    {
        ValidateDto(dto);
        var codigoEmpresa = dto.CodigoEmpresa.Trim();

        var empresa = await _dbContext.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        if (empresa is null || empresa.ManejoCajaDep != 0)
        {
            throw new NotFoundDomainException("Empresa no encontrada o no habilitada para manejo de sesiones en tabla EMPRESA.");
        }

        var parametrosEmpresa = await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        if (parametrosEmpresa is null)
        {
            throw new NotFoundDomainException("La empresa no cuenta con parametros configurados en tabla PARAMETROS_EMPRESA.");
        }

        var fechaServidor = await _dbContext.Database
            .SqlQueryRaw<DateTime>("SELECT GETDATE() AS \"Value\"")
            .FirstAsync(cancellationToken);

        if (dto.FechaApertura.Date > fechaServidor.Date)
        {
            throw new DomainValidationException("La fecha de apertura no puede ser mayor que la fecha actual del servidor.");
        }

        var yaExiste = await _dbContext.SesionesEmpresa
            .AsNoTracking()
            .AnyAsync(x => x.CodigoEmpresa == codigoEmpresa && x.FechaApertura == dto.FechaApertura.Date, cancellationToken);

        if (yaExiste)
        {
            throw new ConflictDomainException("Ya existe una sesion registrada para esa empresa en esa fecha de apertura.");
        }

        var ultimaSesionAbierta = await _dbContext.SesionesEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa && x.StatusSesion == 1)
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(cancellationToken);

        var valorCajaGravar = CalcularValorCajaInicial(parametrosEmpresa.ContMontoCaja, ultimaSesionAbierta?.ValorFinalCaja, empresa);

        _dbContext.SesionesEmpresa.Add(new IniciarCierreSesionDb
        {
            CodigoEmpresa = codigoEmpresa,
            FechaApertura = dto.FechaApertura.Date,
            UsuarioApertura = dto.UsuarioResponsable.Trim(),
            StatusSesion = 1,
            ValorInicialCaja = valorCajaGravar
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo abrir la sesion en tabla INICIO_CIERRE_SESION. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }

        return new SesionAbiertaDto(codigoEmpresa, dto.FechaApertura.Date, valorCajaGravar);
    }

    private static decimal CalcularValorCajaInicial(decimal? contMontoCaja, decimal? valorFinalUltimaSesion, EmpresaDb empresa)
    {
        return contMontoCaja switch
        {
            ManejoCajaDiaAnterior => valorFinalUltimaSesion is > 0 ? valorFinalUltimaSesion.Value : empresa.MontoAuxiliar ?? 0,
            ManejoCajaInicioDia => empresa.MontoInicial ?? 0,
            _ => throw new DomainValidationException("cont_monto_caja de PARAMETROS_EMPRESA tiene un valor no soportado.")
        };
    }

    private static void ValidateDto(AbrirSesionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoEmpresa))
        {
            throw new DomainValidationException("codigo_empresa es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioResponsable))
        {
            throw new DomainValidationException("usuario_responsable es obligatorio.");
        }
    }
}
