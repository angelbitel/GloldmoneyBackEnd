using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.AsignarPinUsuario;

public sealed class AsignarPinUsuarioCommandHandler : IRequestHandler<AsignarPinUsuarioCommand>
{
    private readonly ICuentasUsuarioService _cuentas;

    public AsignarPinUsuarioCommandHandler(ICuentasUsuarioService cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task Handle(AsignarPinUsuarioCommand request, CancellationToken cancellationToken)
    {
        if (!await _cuentas.GuardarPinAsync(request.NombreUsuario.Trim(), request.Pin, DateTime.UtcNow, cancellationToken))
        {
            throw new NotFoundDomainException("Usuario no encontrado.");
        }
    }
}
