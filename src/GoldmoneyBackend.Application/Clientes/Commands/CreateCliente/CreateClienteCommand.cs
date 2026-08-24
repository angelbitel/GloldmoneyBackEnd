using GoldmoneyBackend.Application.Clientes.DTOs;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.CreateCliente;

public sealed record CreateClienteCommand(
    string Nombre,
    string Email,
    string Documento) : IRequest<ClienteDto>;