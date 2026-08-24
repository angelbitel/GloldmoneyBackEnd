using GoldmoneyBackend.Application.Clientes.DTOs;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Queries.GetClienteById;

public sealed record GetClienteByIdQuery(Guid Id) : IRequest<ClienteDto>;