using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.CambiarContrasenaUsuario;

public sealed class CambiarContrasenaUsuarioCommandHandler : IRequestHandler<CambiarContrasenaUsuarioCommand>
{
    private readonly ICuentasUsuarioService _cuentas;

    public CambiarContrasenaUsuarioCommandHandler(ICuentasUsuarioService cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task Handle(CambiarContrasenaUsuarioCommand request, CancellationToken cancellationToken)
    {
        var cambiada = await _cuentas.CambiarContrasenaAsync(
            request.NombreUsuario.Trim(),
            request.NuevaContrasena,
            DateTime.UtcNow,
            cancellationToken);

        if (!cambiada)
        {
            throw new NotFoundDomainException("Usuario no encontrado.");
        }
    }
}
