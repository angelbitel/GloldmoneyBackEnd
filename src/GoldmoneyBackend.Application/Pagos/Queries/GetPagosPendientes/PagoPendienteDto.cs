namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Cuota de interés pendiente de un contrato.
/// </summary>
/// <param name="FechaPago">Fecha hasta la que queda pagado el contrato con esta cuota.</param>
/// <param name="SaldoInteres">Interés que sigue pendiente después de pagar esta cuota.</param>
public sealed record PagoPendienteDto(
    int NumeroPago,
    DateTime FechaPago,
    decimal PagoInteres,
    decimal PagoCapital,
    decimal SaldoCapital,
    decimal SaldoInteres,
    decimal Subtotal);
