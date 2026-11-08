namespace GoldmoneyBackend.Domain.Empenios;

/// <summary>Valores con los que nace un contrato de empeño.</summary>
public sealed record EmpenioCalculado(
    DateTime FechaCreacion,
    DateTime UltimaFechaPago,
    DateTime FechaVencimiento,
    decimal InteresMensual);

/// <summary>
/// Contrato nuevo con las mismas reglas que los contratos del VB6 sin pagos: nace en la fecha
/// de la sesión, "pagado hasta" esa misma fecha, vence a los meses de plazo del grupo y su
/// interés mensual es el capital por la tasa.
/// </summary>
public static class CalculoEmpenio
{
    public static EmpenioCalculado Calcular(decimal capital, decimal tasaMensual, int plazoMeses, DateTime fechaSesion)
    {
        if (capital <= 0)
            throw new ArgumentOutOfRangeException(nameof(capital), "El capital debe ser mayor que cero.");
        if (plazoMeses <= 0)
            throw new ArgumentOutOfRangeException(nameof(plazoMeses), "El plazo debe ser de al menos un mes.");

        var fecha = fechaSesion.Date;

        return new EmpenioCalculado(
            fecha,
            fecha,
            fecha.AddMonths(plazoMeses),
            // SQL Server redondea alejándose de cero al guardar en money/decimal.
            Math.Round(capital * tasaMensual / 100m, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Número de contrato del VB6: empresa + grupo + secuencia del grupo a 10 dígitos
    /// (la serie). Ej.: empresa "1", grupo 1, secuencia 69137 → "110000069137".
    /// </summary>
    public static (string NumeroContrato, string Serie) Numerar(string codigoEmpresa, int codigoGrupo, int secuencia)
    {
        var serie = secuencia.ToString("D10");
        return (string.Concat(codigoEmpresa.Trim(), codigoGrupo.ToString(), serie), serie);
    }
}
