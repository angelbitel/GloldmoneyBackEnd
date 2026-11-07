namespace GoldmoneyBackend.Domain.Pagos;

/// <summary>
/// Reglas de cobro de contratos de empeño replicadas del sistema VB6 legacy
/// (módulo transaccional) y de la base de datos (<c>dbo.Cantidad_de_Meses</c>,
/// <c>dbo.Meses_que_Debe</c>, <c>sp_Actualizar_Saldos_de_la_Empresa</c>).
/// El VB6 sigue operando sobre la misma base, por lo que estos cálculos deben
/// producir exactamente los mismos valores: no "corregirlos" sin acordarlo.
/// </summary>
public static class ReglasCobroLegacy
{
    private const int Decimales = 2;

    /// <summary>
    /// Meses entre dos fechas considerando los días de gracia (port de <c>Cantidad_de_Meses</c> del VB6).
    /// Si el día de <paramref name="fechaSistema"/> supera al día de <paramref name="fechaProceso"/>
    /// en más de <paramref name="diasGracia"/>, el mes en curso cuenta como uno más.
    /// Puede ser negativo cuando <paramref name="fechaProceso"/> es posterior (pago adelantado).
    /// </summary>
    public static int CantidadDeMeses(DateTime fechaProceso, DateTime fechaSistema, int diasGracia)
    {
        // El VB6 trata el último día de febrero como día 30, para que un
        // contrato creado el 28/29-feb no adelante un mes en marzo.
        var diaProceso = fechaProceso.Month == 2 && fechaProceso.Day == DateTime.DaysInMonth(fechaProceso.Year, 2)
            ? 30
            : fechaProceso.Day;

        var dias = fechaSistema.Day - diaProceso;
        var meses = (fechaSistema.Year - fechaProceso.Year) * 12 + (fechaSistema.Month - fechaProceso.Month);

        // El original distingue tres casos (meses > 0, = 0 y < 0) que dan el mismo resultado.
        return dias > diasGracia ? meses + 1 : meses;
    }

    /// <summary>
    /// Cuotas de interés que debe un contrato a la fecha de sesión de la empresa
    /// (port de <c>Meses_que_Debe</c>). Negativo si el contrato tiene pagos adelantados.
    /// </summary>
    public static int MesesQueDebe(DateTime fechaCreacion, DateTime ultimaFechaPago, DateTime fechaSesion, int diasGracia)
    {
        var meses = CantidadDeMeses(ultimaFechaPago, fechaSesion, diasGracia);

        // Contrato sin pagos dentro de su primer mes: debe al menos la primera cuota.
        if (meses == 0 && fechaCreacion.Date == ultimaFechaPago.Date)
        {
            return 1;
        }

        if (CantidadDeMeses(fechaCreacion, fechaSesion, diasGracia) == 0)
        {
            // Dentro del primer mes del contrato se descuenta la cuota en curso.
            return meses < 0 ? meses + 1 : meses - 1;
        }

        return meses;
    }

    /// <summary>
    /// Fecha que resulta de avanzar <paramref name="cantidadMeses"/> meses desde <paramref name="fecha"/>
    /// manteniendo el día de creación del contrato (port de <c>Ultimo_Fecha_Pago</c> del VB6).
    /// Se usa tanto para la última fecha de pago ("pagado hasta") como para el vencimiento.
    /// </summary>
    /// <remarks>
    /// Un contrato creado el 31 cae el 28/29 en febrero; en los meses siguientes vuelve
    /// al día de creación, o al último día del mes si es más corto.
    /// </remarks>
    public static DateTime UltimaFechaPago(DateTime fechaCreacion, DateTime fecha, int cantidadMeses)
    {
        var resultado = fecha.Date.AddMonths(cantidadMeses);

        if (fecha.Day >= 28 && resultado.Day != fechaCreacion.Day)
        {
            var ultimoDiaMes = DateTime.DaysInMonth(resultado.Year, resultado.Month);
            resultado = new DateTime(resultado.Year, resultado.Month, Math.Min(fechaCreacion.Day, ultimoDiaMes));
        }

        return resultado;
    }

    /// <summary>
    /// Interés de un mes sobre el saldo de capital (<c>saldo_capital * (interes / 100)</c>).
    /// <paramref name="tasaMensual"/> es el porcentaje guardado en <c>CONTRATOS.interes</c>.
    /// Sin redondear, igual que la columna <c>money</c> que lo almacena.
    /// </summary>
    public static decimal InteresMensual(decimal saldoCapital, decimal tasaMensual) =>
        saldoCapital * (tasaMensual / 100m);

    /// <summary>
    /// Interés adeudado: interés mensual por los meses que debe. Negativo si hay pagos adelantados
    /// (equivale a <c>saldo_actual - saldo_capital</c> tras <c>sp_Actualizar_Saldos_de_la_Empresa</c>).
    /// </summary>
    public static decimal SaldoInteres(decimal saldoCapital, decimal tasaMensual, int mesesQueDebe) =>
        Redondear(InteresMensual(saldoCapital, tasaMensual) * mesesQueDebe);

    /// <summary>Abono a interés por <paramref name="cuotas"/> cuotas (cuotas × interés mensual).</summary>
    public static decimal AbonoInteres(decimal saldoCapital, decimal tasaMensual, int cuotas) =>
        Redondear(InteresMensual(saldoCapital, tasaMensual) * cuotas);

    /// <summary>Redondeo a centavos que aplica SQL Server al convertir a <c>decimal(8,2)</c>.</summary>
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, Decimales, MidpointRounding.AwayFromZero);
}
