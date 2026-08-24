using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.DeleteCliente;

public sealed record DeleteClienteCommand(Guid Id) : IRequest;