namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Respuesta con la lista de pagos pendientes de un contrato y el valor total a pagar.
/// </summary>
public sealed record PagosPendientesDto(
    string ContratoId,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    int CantidadPagos,
    IReadOnlyList<PagoPendienteDto> Pagos,
    decimal TotalPagar);