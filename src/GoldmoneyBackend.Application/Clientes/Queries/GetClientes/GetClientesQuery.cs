using GoldmoneyBackend.Application.Clientes.DTOs;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Queries.GetClientes;

public sealed record GetClientesQuery : IRequest<IReadOnlyList<ClienteDto>>;