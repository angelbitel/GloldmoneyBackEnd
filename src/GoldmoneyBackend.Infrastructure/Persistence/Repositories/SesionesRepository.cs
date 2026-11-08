using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
            // Como en el VB6 (y en cobros y empeños): la última sesión está abierta mientras no
            // tenga hora de cierre. status_sesion = 1 marca las sesiones ya cerradas.
            var ultimaSesion = await _dbContext.SesionesEmpresa
                .AsNoTracking()
                .Where(x => x.CodigoEmpresa == empresa.CodigoEmpresa)
                .OrderByDescending(x => x.FechaApertura)
                .FirstOrDefaultAsync(cancellationToken);

            resultado.Add(new EstadoSesionEmpresaDto(
                empresa.CodigoEmpresa,
                empresa.NombreEmpresa,
                ultimaSesion is { HoraCierre: null },
                ultimaSesion?.FechaApertura));
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

        // Como el VB6: la sesión es del día local de la empresa, no de la fecha que mande el equipo.
        var hoy = OperacionesCajaLegacy.AhoraLocal().Date;

        return await OperacionesCajaLegacy.EnTransaccionAsync(_dbContext, async ct =>
        {
            // UPDLOCK + HOLDLOCK: dos aperturas simultáneas de la misma empresa se ejecutan una tras otra.
            var ultimaSesion = await _dbContext.SesionesEmpresa
                .FromSqlInterpolated($"""
                    SELECT TOP (1) * FROM INICIO_CIERRE_SESION WITH (UPDLOCK, HOLDLOCK)
                    WHERE codigo_empresa = {codigoEmpresa}
                    ORDER BY fecha_apertura DESC
                    """)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            // Una sola caja abierta por empresa: abierta = última sesión sin hora de cierre.
            if (ultimaSesion is { HoraCierre: null })
            {
                throw new ConflictDomainException(
                    $"La caja de la empresa ya está abierta (sesión del {ultimaSesion.FechaApertura:dd/MM/yyyy}). " +
                    "Debe cerrarse antes de abrir otra.");
            }

            // Una sesión por día y siempre posterior a la última (clave codigo_empresa + fecha_apertura).
            if (ultimaSesion is not null && ultimaSesion.FechaApertura.Date >= hoy)
            {
                throw new ConflictDomainException(
                    $"La empresa ya tuvo una sesión el {ultimaSesion.FechaApertura:dd/MM/yyyy}; la próxima caja se abre otro día.");
            }

            var valorInicial = CalcularValorCajaInicial(parametrosEmpresa.ContMontoCaja, ultimaSesion?.ValorFinalCaja, empresa);

            // sp_Gravar_Sesiones_Empresa: status 0 = abierta, sin hora ni usuario de cierre.
            _dbContext.SesionesEmpresa.Add(new IniciarCierreSesionDb
            {
                CodigoEmpresa = codigoEmpresa,
                FechaApertura = hoy,
                UsuarioApertura = dto.UsuarioResponsable,
                StatusSesion = 0,
                ValorInicialCaja = valorInicial
            });
            await _dbContext.SaveChangesAsync(ct);

            // También como sp_Gravar_Sesiones_Empresa: la caja de la empresa arranca en el valor inicial.
            await _dbContext.Empresas
                .Where(x => x.CodigoEmpresa == codigoEmpresa)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MontoAuxiliar, valorInicial), ct);

            await ActualizarSaldosDeLaEmpresaAsync(codigoEmpresa, ct);

            return new SesionAbiertaDto(codigoEmpresa, hoy, valorInicial);
        }, cancellationToken);
    }

    /// <summary>
    /// "Iniciar día" del VB6 (sp_Actualizar_Saldos_de_la_Empresa): recalcula saldo_actual e
    /// interés mensual de los contratos a la fecha de la sesión recién abierta. Se ejecuta el
    /// procedimiento original para que el resultado sea idéntico al del VB6.
    /// </summary>
    private async Task ActualizarSaldosDeLaEmpresaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var conexion = _dbContext.Database.GetDbConnection();
        await using var comando = conexion.CreateCommand();
        comando.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        comando.CommandText = "EXEC sp_Actualizar_Saldos_de_la_Empresa @codigo_empresa";

        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@codigo_empresa";
        parametro.Value = codigoEmpresa;
        comando.Parameters.Add(parametro);

        var estado = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
        if (estado != 0)
        {
            throw new DomainValidationException("No se pudieron actualizar los saldos de los contratos al abrir la caja.");
        }
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
