using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.ActualizarCorreoUsuario;

public sealed class ActualizarCorreoUsuarioCommandHandler : IRequestHandler<ActualizarCorreoUsuarioCommand>
{
    private readonly ICuentasUsuarioService _cuentas;

    public ActualizarCorreoUsuarioCommandHandler(ICuentasUsuarioService cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task Handle(ActualizarCorreoUsuarioCommand request, CancellationToken cancellationToken)
    {
        var nombreUsuario = request.NombreUsuario.Trim();

        if (!await _cuentas.UsuarioExisteAsync(nombreUsuario, cancellationToken))
        {
            throw new NotFoundDomainException("Usuario no encontrado.");
        }

        var correo = string.IsNullOrWhiteSpace(request.Correo)
            ? null
            : request.Correo.Trim().ToLowerInvariant();

        await _cuentas.GuardarCorreoAsync(nombreUsuario, correo, cancellationToken);
    }
}
