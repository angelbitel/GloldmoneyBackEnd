namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Genera los datos a pagar de un contrato replicando en C# el flujo del
/// procedimiento almacenado legacy <c>sp_ad_abono_interes</c>:
/// valida el contrato en CONTRATOS (equivalente al cursor) y calcula los
/// importes que el SP recibe como parámetros (pago_interes, pago_capital,
/// saldo_capital, saldo_interes).
/// </summary>
public static class CalculadorPagosPendientes
{
    private const int Decimales = 2;

    /// <summary>Calcula la lista de pagos pendientes y el total a pagar de un contrato.</summary>
    public static PagosPendientesDto Calcular(ContratoPagoDto contrato)
    {
        var referencia = contrato.UltimaFechaPago ?? contrato.FechaCreacion ?? DateTime.Today;
        var vencimiento = contrato.FechaVencimiento;

        // Sin fecha de vencimiento o vencimiento ya alcanzado: no hay pagos pendientes.
        if (vencimiento is null || vencimiento.Value <= referencia)
        {
            return SinPagos(contrato);
        }

        var cantidadPagos = CalcularMesesPendientes(referencia, vencimiento.Value);
        var tasaMensual = contrato.Interes / 100m;
        var saldoCapital = contrato.SaldoCapital;
        var totalPagar = 0m;
        var pagos = new List<PagoPendienteDto>(cantidadPagos);

        for (var i = 1; i <= cantidadPagos; i++)
        {
            var fechaPago = referencia.AddMonths(i);
            if (fechaPago > vencimiento.Value)
            {
                fechaPago = vencimiento.Value;
            }

            // Abono de interés: interés mensual sobre el saldo de capital vigente.
            var pagoInteres = Redondear(saldoCapital * tasaMensual);

            // El resto de la mensualidad se aplica a capital; si el contrato no
            // tiene mensualidad definida, el abono cubre únicamente el interés.
            var pagoCapital = contrato.InteresMensual > 0m
                ? Math.Max(0m, Redondear(contrato.InteresMensual - pagoInteres))
                : 0m;

            var subtotal = Redondear(pagoInteres + pagoCapital);
            saldoCapital = Math.Max(0m, Redondear(saldoCapital - pagoCapital));

            // Se asume pago puntual del interés del mes, por lo que el saldo de
            // interés queda en cero tras cada abono.
            var saldoInteres = 0m;

            totalPagar = Redondear(totalPagar + subtotal);

            pagos.Add(new PagoPendienteDto(
                i,
                fechaPago,
                pagoInteres,
                pagoCapital,
                saldoCapital,
                saldoInteres,
                subtotal));
        }

        return new PagosPendientesDto(
            ConstruirContratoId(contrato),
            contrato.CodigoEmpresa,
            contrato.CodigoGrupo,
            contrato.NumeroContrato,
            pagos.Count,
            pagos,
            totalPagar);
    }

    private static PagosPendientesDto SinPagos(ContratoPagoDto contrato) =>
        new(
            ConstruirContratoId(contrato),
            contrato.CodigoEmpresa,
            contrato.CodigoGrupo,
            contrato.NumeroContrato,
            0,
            Array.Empty<PagoPendienteDto>(),
            0m);

    /// <summary>
    /// Cantidad de cuotas mensuales entre la referencia (última fecha de pago o
    /// fecha de creación) y la fecha de vencimiento del contrato.
    /// </summary>
    private static int CalcularMesesPendientes(DateTime referencia, DateTime vencimiento)
    {
        var meses = (vencimiento.Year - referencia.Year) * 12 + (vencimiento.Month - referencia.Month);

        // Si el día de vencimiento es anterior al día de referencia, aún no se
        // cumple el mes completo.
        if (vencimiento.Day < referencia.Day)
        {
            meses--;
        }

        return Math.Max(1, meses);
    }

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, Decimales, MidpointRounding.AwayFromZero);

    private static string ConstruirContratoId(ContratoPagoDto contrato) =>
        contrato.CodigoEmpresa.Trim() + contrato.CodigoGrupo.ToString() + contrato.NumeroContrato.Trim();
}