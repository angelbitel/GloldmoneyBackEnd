using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Domain.Pagos;

/// <summary>Datos actuales del contrato necesarios para calcular un abono.</summary>
public sealed record ContratoParaAbono(
    DateTime FechaCreacion,
    DateTime UltimaFechaPago,
    DateTime FechaVencimiento,
    decimal SaldoCapital,
    decimal TasaMensual);

/// <summary>Resultado de un abono: importes cobrados y valores nuevos del contrato.</summary>
public sealed record AbonoCalculado(
    int MesesQueDebe,
    int Cuotas,
    decimal AbonoInteres,
    decimal AbonoCapital,
    decimal Total,
    decimal NuevoSaldoCapital,
    decimal NuevoSaldoInteres,
    decimal NuevoInteresMensual,
    DateTime NuevaUltimaFechaPago,
    DateTime NuevoVencimiento);

/// <summary>
/// Abono a interés y/o capital de un contrato, con las mismas reglas y validaciones
/// que <c>Salvar_Abono_de_Interes</c> del VB6.
/// </summary>
public static class CalculoAbono
{
    public static AbonoCalculado Calcular(
        ContratoParaAbono contrato,
        DateTime fechaSesion,
        int diasGracia,
        int cuotas,
        decimal abonoCapital)
    {
        if (cuotas < 0 || abonoCapital < 0)
        {
            throw new DomainValidationException("Estimado usuario, las cuotas y el abono a capital no pueden ser negativos.");
        }

        var mesesQueDebe = ReglasCobroLegacy.MesesQueDebe(
            contrato.FechaCreacion, contrato.UltimaFechaPago, fechaSesion, diasGracia);

        if (cuotas == 0)
        {
            if (mesesQueDebe > 0)
            {
                throw new DomainValidationException("Estimado usuario, con meses adeudados no es posible dejar la casilla de meses a pagar en blanco.");
            }

            if (abonoCapital == 0)
            {
                throw new DomainValidationException("Estimado usuario, si no ha ingresado una cantidad de pagos y el contrato no tiene deuda pendiente, entonces el abono a capital no puede estar en blanco.");
            }
        }

        if (abonoCapital > 0 && cuotas < mesesQueDebe)
        {
            throw new DomainValidationException("Estimado usuario, el abono a capital no puede ser registrado porque la deuda supera los meses a pagar.");
        }

        // El VB6 no lo valida y dejaría el saldo en cero o negativo; cancelar el capital es un retiro.
        if (abonoCapital >= contrato.SaldoCapital)
        {
            throw new DomainValidationException("Estimado usuario, el abono a capital debe ser menor que el saldo de capital. Para cancelar el contrato registre un retiro.");
        }

        var abonoInteres = ReglasCobroLegacy.AbonoInteres(contrato.SaldoCapital, contrato.TasaMensual, cuotas);
        var saldoInteres = ReglasCobroLegacy.SaldoInteres(contrato.SaldoCapital, contrato.TasaMensual, mesesQueDebe);
        var nuevoSaldoCapital = contrato.SaldoCapital - abonoCapital;

        return new AbonoCalculado(
            mesesQueDebe,
            cuotas,
            abonoInteres,
            abonoCapital,
            abonoInteres + abonoCapital,
            nuevoSaldoCapital,
            saldoInteres - abonoInteres,
            ReglasCobroLegacy.Redondear(ReglasCobroLegacy.InteresMensual(nuevoSaldoCapital, contrato.TasaMensual)),
            ReglasCobroLegacy.UltimaFechaPago(contrato.FechaCreacion, contrato.UltimaFechaPago, cuotas),
            ReglasCobroLegacy.UltimaFechaPago(contrato.FechaCreacion, contrato.FechaVencimiento, cuotas));
    }
}
