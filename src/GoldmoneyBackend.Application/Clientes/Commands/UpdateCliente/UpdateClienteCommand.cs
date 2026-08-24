using GoldmoneyBackend.Application.Clientes.DTOs;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.UpdateCliente;

public sealed record UpdateClienteCommand(
    Guid Id,
    string Nombre,
    string Email,
    string Documento) : IRequest<ClienteDto>;