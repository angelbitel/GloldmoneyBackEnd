using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed record GetContratoCompletoQuery(string ContratoId) : IRequest<ContratoCompletoDto?>;