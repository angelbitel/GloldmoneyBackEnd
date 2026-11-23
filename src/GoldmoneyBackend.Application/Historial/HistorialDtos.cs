namespace GoldmoneyBackend.Application.Historial;

/// <summary>Un pago (abono o retiro) de un contrato, activo (PAGOS) o ya en el historial (HISTORIAL_PAGOS).</summary>
/// <param name="PagadoHasta">Última fecha cubierta por el pago (ultima_fecha_pago).</param>
public sealed record PagoHistorialDto(
    int NumeroPago,
    DateTime FechaPago,
    int CantidadPagos,
    decimal PagoInteres,
    decimal PagoCapital,
    decimal SaldoCapital,
    DateTime? PagadoHasta,
    int SecuenciaRecibo,
    string? UsuarioResponsable);

/// <summary>Contrato que ya salió de CONTRATOS (HISTORIAL_CONTRATOS) con los totales de sus pagos.</summary>
/// <param name="Status">RC = retirado por el cliente, CF = rematado (retiro casa), AC = anulado.</param>
/// <param name="FechaSalida">Fecha en que pasó al historial (fecha_ingreso).</param>
public sealed record ContratoHistorialDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string Status,
    DateTime? FechaCreacion,
    DateTime? FechaVencimiento,
    DateTime? FechaSalida,
    DateTime? UltimaFechaPago,
    decimal CapitalPrestado,
    decimal InteresMensual,
    decimal SaldoCapital,
    decimal Saldo,
    int CuotasPagadas,
    decimal InteresesPagados,
    decimal CapitalAbonado);

public sealed record HistorialClienteDto(
    IReadOnlyList<ContratoHistorialDto> Retirados,
    IReadOnlyList<ContratoHistorialDto> Rematados,
    IReadOnlyList<ContratoHistorialDto> Anulados);

/// <summary>Métricas de cartera de una empresa (o de un cliente dentro de ella).</summary>
/// <param name="InteresPendiente">Saldo actual menos saldo de capital de los contratos activos.</param>
public sealed record MetricasCarteraDto(
    int ContratosActivos,
    int ContratosVencidos,
    decimal CapitalColocado,
    decimal InteresPendiente,
    decimal DeudaTotal,
    int Retirados,
    int Rematados,
    int Anulados,
    DateTime FechaSesion);
