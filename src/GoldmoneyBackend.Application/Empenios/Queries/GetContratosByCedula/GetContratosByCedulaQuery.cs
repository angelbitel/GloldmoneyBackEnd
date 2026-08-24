using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratosByCedula;

public sealed record GetContratosByCedulaQuery(string Cedula) : IRequest<IReadOnlyList<ContratoDto>>;
