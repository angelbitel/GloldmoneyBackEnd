namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Respuesta con la lista de cuotas de interés pendientes de un contrato y el valor total a pagar.
/// </summary>
/// <param name="MesesQueDebe">Meses que debe a la fecha de sesión; negativo si tiene pagos adelantados.</param>
/// <param name="FechaSesion">Fecha de trabajo de la empresa usada para el cálculo.</param>
public sealed record PagosPendientesDto(
    string ContratoId,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    int CantidadPagos,
    IReadOnlyList<PagoPendienteDto> Pagos,
    decimal TotalPagar,
    int MesesQueDebe,
    decimal InteresMensual,
    DateTime FechaSesion);
