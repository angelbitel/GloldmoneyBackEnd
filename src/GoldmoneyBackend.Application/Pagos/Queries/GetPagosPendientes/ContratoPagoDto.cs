namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Datos del contrato necesarios para generar los pagos pendientes.
/// </summary>
public sealed record ContratoPagoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    DateTime? FechaCreacion,
    decimal Interes,
    decimal InteresMensual,
    DateTime? UltimaFechaPago,
    decimal SaldoCapital,
    DateTime? FechaVencimiento);