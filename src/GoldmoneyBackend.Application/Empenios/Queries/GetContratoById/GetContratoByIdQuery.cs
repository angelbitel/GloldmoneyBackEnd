using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed record GetContratoByIdQuery(string ContratoId) : IRequest<ContratoDto>;
