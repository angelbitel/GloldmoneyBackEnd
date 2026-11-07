namespace GoldmoneyBackend.Application.Pagos.Queries.GetContratosCobro;

/// <summary>
/// Contrato activo de un cliente con lo que debe a la fecha de sesión
/// (equivale a una fila de la grilla de "Registro de pagos" del VB6).
/// </summary>
/// <param name="Tasa">Tasa mensual en porcentaje.</param>
/// <param name="InteresMensual">Interés de un mes sobre el saldo de capital (lo que cuesta cada cuota).</param>
/// <param name="MesesQueDebe">Cuotas de interés vencidas ("# de Pagos"); negativo si tiene pagos adelantados.</param>
/// <param name="SaldoInteres">Interés adeudado (MesesQueDebe × InteresMensual); negativo si hay pagos adelantados.</param>
/// <param name="SaldoTotal">Saldo de capital + saldo de interés (lo que cuesta retirar la prenda).</param>
public sealed record ContratoCobroDto(
    string ContratoId,
    string CodigoEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string NumeroContrato,
    string IdCliente,
    decimal CapitalPrestado,
    decimal SaldoCapital,
    decimal Tasa,
    decimal InteresMensual,
    int MesesQueDebe,
    decimal SaldoInteres,
    decimal SaldoTotal,
    int PlazoPago,
    DateTime? FechaCreacion,
    DateTime? UltimaFechaPago,
    DateTime? FechaVencimiento,
    DateTime FechaSesion);
