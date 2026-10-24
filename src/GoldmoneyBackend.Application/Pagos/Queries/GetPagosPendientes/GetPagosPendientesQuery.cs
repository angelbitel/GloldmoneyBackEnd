using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

/// <summary>
/// Consulta los pagos pendientes y el valor total a pagar de un contrato.
/// </summary>
/// <param name="ContratoId">Identificador compuesto: codigoEmpresa + codigoGrupo + numeroContrato.</param>
public sealed record GetPagosPendientesQuery(string ContratoId) : IRequest<PagosPendientesDto>;