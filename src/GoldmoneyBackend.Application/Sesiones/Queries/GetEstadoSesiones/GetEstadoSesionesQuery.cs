using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Queries.GetEstadoSesiones;

public sealed record GetEstadoSesionesQuery : IRequest<IReadOnlyList<EstadoSesionEmpresaDto>>;
