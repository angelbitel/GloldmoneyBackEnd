using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.RestablecerContrasena;

public sealed class RestablecerContrasenaCommandHandler : IRequestHandler<RestablecerContrasenaCommand>
{
    private readonly ICuentasUsuarioService _cuentas;

    public RestablecerContrasenaCommandHandler(ICuentasUsuarioService cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task Handle(RestablecerContrasenaCommand request, CancellationToken cancellationToken)
    {
        var restablecida = await _cuentas.RestablecerContrasenaAsync(
            TokenRestablecimiento.Hash(request.Token.Trim()),
            request.NuevaContrasena,
            DateTime.UtcNow,
            cancellationToken);

        if (!restablecida)
        {
            throw new DomainValidationException("El enlace no es válido o ya venció. Solicita uno nuevo.");
        }
    }
}
