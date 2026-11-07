using GoldmoneyBackend.Application.Pagos.Queries.GetContratosCobro;
using GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;
using GoldmoneyBackend.Domain.Pagos;

namespace GoldmoneyBackend.Application.Pagos;

/// <summary>
/// Calcula lo que debe un contrato con las reglas del sistema legacy
/// (<see cref="ReglasCobroLegacy"/>), a la fecha de sesión de la empresa.
/// </summary>
public static class CalculadorCobro
{
    /// <summary>Fila de la grilla de cobro (equivale a la grilla del módulo transaccional VB6).</summary>
    public static ContratoCobroDto ConstruirFila(ContratoPagoDto contrato)
    {
        var mesesQueDebe = MesesQueDebe(contrato);
        var saldoInteres = ReglasCobroLegacy.SaldoInteres(contrato.SaldoCapital, contrato.Interes, mesesQueDebe);

        return new ContratoCobroDto(
            ConstruirContratoId(contrato),
            contrato.CodigoEmpresa.Trim(),
            contrato.CodigoGrupo,
            contrato.AbreviaturaGrupo?.Trim().ToUpperInvariant(),
            contrato.NumeroContrato.Trim(),
            contrato.IdCliente.Trim(),
            contrato.CapitalPrestado,
            contrato.SaldoCapital,
            contrato.Interes,
            ReglasCobroLegacy.Redondear(ReglasCobroLegacy.InteresMensual(contrato.SaldoCapital, contrato.Interes)),
            mesesQueDebe,
            saldoInteres,
            contrato.SaldoCapital + saldoInteres,
            contrato.PlazoPago,
            contrato.FechaCreacion,
            contrato.UltimaFechaPago,
            contrato.FechaVencimiento,
            contrato.FechaSesion.Date);
    }

    /// <summary>
    /// Cuotas de interés pendientes a la fecha de sesión. Sin cuotas si el contrato
    /// está al día o adelantado (meses que debe &lt;= 0).
    /// </summary>
    public static PagosPendientesDto CalcularPagosPendientes(ContratoPagoDto contrato)
    {
        var mesesQueDebe = MesesQueDebe(contrato);
        var cuotas = Math.Max(0, mesesQueDebe);
        var interesMensual = ReglasCobroLegacy.InteresMensual(contrato.SaldoCapital, contrato.Interes);
        var pagos = new List<PagoPendienteDto>(cuotas);

        for (var i = 1; i <= cuotas; i++)
        {
            pagos.Add(new PagoPendienteDto(
                i,
                FechaCuota(contrato, i),
                ReglasCobroLegacy.Redondear(interesMensual),
                0m,
                contrato.SaldoCapital,
                ReglasCobroLegacy.SaldoInteres(contrato.SaldoCapital, contrato.Interes, cuotas - i),
                ReglasCobroLegacy.Redondear(interesMensual)));
        }

        return new PagosPendientesDto(
            ConstruirContratoId(contrato),
            contrato.CodigoEmpresa.Trim(),
            contrato.CodigoGrupo,
            contrato.NumeroContrato.Trim(),
            pagos.Count,
            pagos,
            ReglasCobroLegacy.AbonoInteres(contrato.SaldoCapital, contrato.Interes, cuotas),
            mesesQueDebe,
            ReglasCobroLegacy.Redondear(interesMensual),
            contrato.FechaSesion.Date);
    }

    private static int MesesQueDebe(ContratoPagoDto contrato)
    {
        // En el legacy ambas fechas siempre existen: al crear el contrato la
        // última fecha de pago es la fecha de creación.
        var fechaCreacion = contrato.FechaCreacion ?? contrato.UltimaFechaPago;
        var ultimaFechaPago = contrato.UltimaFechaPago ?? contrato.FechaCreacion;

        if (fechaCreacion is null || ultimaFechaPago is null)
        {
            return 0;
        }

        return ReglasCobroLegacy.MesesQueDebe(
            fechaCreacion.Value,
            ultimaFechaPago.Value,
            contrato.FechaSesion,
            contrato.DiasGracia);
    }

    /// <summary>Fecha hasta la que queda pagado el contrato tras la cuota <paramref name="numeroCuota"/>.</summary>
    private static DateTime FechaCuota(ContratoPagoDto contrato, int numeroCuota)
    {
        var ultimaFechaPago = (contrato.UltimaFechaPago ?? contrato.FechaCreacion)!.Value;
        var fechaCreacion = contrato.FechaCreacion ?? ultimaFechaPago;

        return ReglasCobroLegacy.UltimaFechaPago(fechaCreacion, ultimaFechaPago, numeroCuota);
    }

    private static string ConstruirContratoId(ContratoPagoDto contrato) =>
        contrato.CodigoEmpresa.Trim() + contrato.CodigoGrupo.ToString() + contrato.NumeroContrato.Trim();
}
