using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Graba abonos igual que el VB6 (<c>Salvar_Abono_de_Interes</c> y los procedimientos
/// <c>sp_ad_abono_interes</c>, <c>sp_actualizar_campos_contratos</c>, <c>sp_Actualizar_Caja_Empresa</c>,
/// <c>sp_ad_Movimiento_Caja</c> y <c>sp_GenerarSecuencia</c>). Ver <see cref="OperacionesCajaLegacy"/>.
/// </summary>
public sealed class AbonosRepository : IAbonosRepository
{
    private const string SecuenciaPagos = "AI";
    private const string TransaccionAbonoInteres = "AI";
    private const string TransaccionAbonoCapital = "AP";

    private readonly LegacyDataDbContext _dbContext;

    public AbonosRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken) =>
        OperacionesCajaLegacy.EnTransaccionAsync(_dbContext, operacion, cancellationToken);

    public async Task<ContratoAbonoDto?> GetContratoBloqueadoAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        // El segundo de dos cobros simultáneos calcula sobre los saldos ya actualizados.
        var contrato = await OperacionesCajaLegacy.GetContratoBloqueadoAsync(_dbContext, codigoEmpresa, codigoGrupo, numeroContrato, cancellationToken);

        if (contrato is null)
        {
            return null;
        }

        return new ContratoAbonoDto(
            contrato.CodigoEmpresa.Trim(),
            contrato.CodigoGrupo,
            contrato.NumeroContrato.Trim(),
            contrato.FechaCreacion,
            contrato.UltimaFechaPago,
            contrato.FechaVencimiento,
            contrato.SaldoCapital ?? 0m,
            contrato.Interes ?? 0m);
    }

    public async Task<SesionCobroDto> GetSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var (fechaSesion, diasGracia) = await OperacionesCajaLegacy.GetSesionAbiertaAsync(_dbContext, codigoEmpresa, cancellationToken);
        return new SesionCobroDto(fechaSesion, diasGracia);
    }

    public async Task<NumerosAbonoDto> RegistrarAbonoAsync(RegistroAbonoDto registro, CancellationToken cancellationToken)
    {
        var abono = registro.Abono;
        var ahora = OperacionesCajaLegacy.AhoraLocal();

        // 1. Secuencias del pago y del recibo.
        var numeroPago = await OperacionesCajaLegacy.SiguienteSecuenciaAsync(_dbContext, registro.CodigoEmpresa, SecuenciaPagos, cancellationToken);
        var numeroRecibo = await OperacionesCajaLegacy.SiguienteSecuenciaAsync(_dbContext, registro.CodigoEmpresa, OperacionesCajaLegacy.SecuenciaRecibos, cancellationToken);

        // 2. Pago (sp_ad_abono_interes).
        _dbContext.Pagos.Add(new PagoDb
        {
            CodigoEmpresa = registro.CodigoEmpresa,
            NumeroPagos = numeroPago,
            CodigoGrupo = registro.CodigoGrupo,
            NumeroContrato = registro.NumeroContrato,
            FechaPago = registro.FechaSesion,
            CantidadPagos = abono.Cuotas,
            PagoInteres = abono.AbonoInteres,
            PagoCapital = abono.AbonoCapital,
            SaldoCapital = abono.NuevoSaldoCapital,
            SaldoInteres = abono.NuevoSaldoInteres,
            UltimaFechaPago = abono.NuevaUltimaFechaPago,
            UsuarioResponsable = registro.UsuarioResponsable,
            SecuenciaRecibo = numeroRecibo,
            HoraTransaccion = ahora
        });

        // 3. Contrato (sp_actualizar_campos_contratos). Ya está cargado y bloqueado por GetContratoBloqueadoAsync.
        var contrato = await _dbContext.Contratos
            .FirstAsync(x => x.CodigoEmpresa == registro.CodigoEmpresa
                && x.CodigoGrupo == registro.CodigoGrupo
                && x.NumeroContrato == registro.NumeroContrato, cancellationToken);

        contrato.InteresMensual = abono.NuevoInteresMensual;
        contrato.SaldoCapital = abono.NuevoSaldoCapital;
        contrato.SaldoActual = abono.NuevoSaldoInteres + abono.NuevoSaldoCapital;
        contrato.UltimaFechaPago = abono.NuevaUltimaFechaPago;
        contrato.FechaVencimiento = abono.NuevoVencimiento;

        // 4. Caja de la empresa (sp_Actualizar_Caja_Empresa, incremento).
        await OperacionesCajaLegacy.ActualizarCajaEmpresaAsync(_dbContext, registro.CodigoEmpresa, abono.Total, cancellationToken);

        // 5. Movimientos de caja: uno por el interés y otro por el capital, si los hay.
        if (abono.AbonoInteres > 0)
        {
            await AgregarMovimientoAsync(registro, TransaccionAbonoInteres, abono.AbonoInteres, ahora, cancellationToken);
        }

        if (abono.AbonoCapital > 0)
        {
            await AgregarMovimientoAsync(registro, TransaccionAbonoCapital, abono.AbonoCapital, ahora, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new NumerosAbonoDto(numeroPago, numeroRecibo);
    }

    private Task<int> AgregarMovimientoAsync(RegistroAbonoDto registro, string codigoTransaccion, decimal monto, DateTime ahora, CancellationToken cancellationToken) =>
        OperacionesCajaLegacy.AgregarMovimientoAsync(
            _dbContext,
            registro.CodigoEmpresa,
            registro.CodigoGrupo,
            registro.NumeroContrato,
            codigoTransaccion,
            monto,
            registro.FechaSesion,
            ahora,
            registro.UsuarioResponsable,
            cancellationToken);
}
