namespace GoldmoneyBackend.Domain.Pagos;

/// <summary>Datos actuales del contrato necesarios para calcular su retiro.</summary>
public sealed record ContratoParaRetiro(
    DateTime FechaCreacion,
    DateTime UltimaFechaPago,
    DateTime FechaVencimiento,
    decimal SaldoCapital,
    decimal TasaMensual);

/// <summary>
/// Resultado de un retiro. <see cref="PagoInteres"/> es negativo si el contrato tenía pagos
/// adelantados; <see cref="Total"/> es lo que entra en caja.
/// </summary>
public sealed record RetiroCalculado(
    int MesesQueDebe,
    decimal PagoCapital,
    decimal PagoInteres,
    decimal Total,
    DateTime NuevaUltimaFechaPago,
    DateTime NuevoVencimiento);

/// <summary>Retiro de un contrato, con los mismos importes que <c>Salvar_Retiro_de_Contrato</c> del VB6.</summary>
public static class CalculoRetiro
{
    /// <param name="restarAbonoCapital">
    /// Parámetro <c>restar_abono_capital</c> de la empresa: si está activo, el interés pagado por
    /// adelantado (negativo) se descuenta del capital a cobrar; si no, solo se cobra el capital.
    /// </param>
    public static RetiroCalculado Calcular(
        ContratoParaRetiro contrato,
        DateTime fechaSesion,
        int diasGracia,
        bool restarAbonoCapital)
    {
        var mesesQueDebe = ReglasCobroLegacy.MesesQueDebe(
            contrato.FechaCreacion, contrato.UltimaFechaPago, fechaSesion, diasGracia);

        var pagoCapital = contrato.SaldoCapital;
        var pagoInteres = ReglasCobroLegacy.SaldoInteres(contrato.SaldoCapital, contrato.TasaMensual, mesesQueDebe);
        var total = restarAbonoCapital || pagoInteres >= 0 ? pagoCapital + pagoInteres : pagoCapital;

        // El VB6 corre las fechas por los meses que debe (hacia atrás si estaba adelantado)
        // antes de pasar el contrato al historial.
        return new RetiroCalculado(
            mesesQueDebe,
            pagoCapital,
            pagoInteres,
            total,
            ReglasCobroLegacy.UltimaFechaPago(contrato.FechaCreacion, contrato.UltimaFechaPago, mesesQueDebe),
            ReglasCobroLegacy.UltimaFechaPago(contrato.FechaCreacion, contrato.FechaVencimiento, mesesQueDebe));
    }
}
