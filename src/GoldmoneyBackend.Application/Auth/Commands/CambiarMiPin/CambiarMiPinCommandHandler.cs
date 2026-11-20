using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.CambiarMiPin;

public sealed class CambiarMiPinCommandHandler : IRequestHandler<CambiarMiPinCommand>
{
    private readonly IAuthService _auth;
    private readonly ICuentasUsuarioService _cuentas;

    public CambiarMiPinCommandHandler(IAuthService auth, ICuentasUsuarioService cuentas)
    {
        _auth = auth;
        _cuentas = cuentas;
    }

    public async Task Handle(CambiarMiPinCommand request, CancellationToken cancellationToken)
    {
        // Se valida la contraseña con el mismo login: sistema nuevo, VB6 o usuarios de respaldo.
        if (await _auth.LoginAsync(request.NombreUsuario, request.Contrasena, cancellationToken) is null)
        {
            throw new DomainValidationException("La contraseña no es correcta.");
        }

        if (!await _cuentas.GuardarPinAsync(request.NombreUsuario.Trim(), request.Pin, DateTime.UtcNow, cancellationToken))
        {
            throw new NotFoundDomainException("Usuario no encontrado.");
        }
    }
}
