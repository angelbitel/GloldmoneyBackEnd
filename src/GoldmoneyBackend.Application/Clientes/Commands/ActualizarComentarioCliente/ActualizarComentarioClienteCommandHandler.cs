using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.ActualizarComentarioCliente;

public sealed class ActualizarComentarioClienteCommandHandler : IRequestHandler<ActualizarComentarioClienteCommand>
{
    private readonly IClientesRepository _clientes;

    public ActualizarComentarioClienteCommandHandler(IClientesRepository clientes)
    {
        _clientes = clientes;
    }

    public async Task Handle(ActualizarComentarioClienteCommand request, CancellationToken cancellationToken)
    {
        var comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim();

        if (!await _clientes.ActualizarComentarioAsync(request.IdCliente.Trim(), comentario, cancellationToken))
        {
            throw new NotFoundDomainException("Cliente no encontrado.");
        }
    }
}
