namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Cuota pendiente de un contrato (desglose interés / capital / saldos).
/// </summary>
public sealed record PagoPendienteDto(
    int NumeroPago,
    DateTime FechaPago,
    decimal PagoInteres,
    decimal PagoCapital,
    decimal SaldoCapital,
    decimal SaldoInteres,
    decimal Subtotal);