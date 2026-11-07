using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetContratosCobro;

/// <summary>
/// Consulta los contratos activos de un cliente con lo que debe cada uno a la fecha de sesión.
/// </summary>
public sealed record GetContratosCobroQuery(string IdCliente) : IRequest<IReadOnlyList<ContratoCobroDto>>;
