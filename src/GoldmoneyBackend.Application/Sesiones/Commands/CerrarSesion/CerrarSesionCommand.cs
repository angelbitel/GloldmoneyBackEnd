using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.CerrarSesion;

/// <summary>Cierre de caja de la empresa. Requiere la contraseña del usuario conectado o de un administrador.</summary>
public sealed record CerrarSesionCommand(string CodigoEmpresa, string Usuario, string Clave, string UsuarioConectado)
    : IRequest<SesionCerradaDto>;

public sealed class CerrarSesionCommandHandler : IRequestHandler<CerrarSesionCommand, SesionCerradaDto>
{
    private readonly ISesionesRepository _sesiones;
    private readonly ITransaccionesContratoRepository _transacciones;

    public CerrarSesionCommandHandler(ISesionesRepository sesiones, ITransaccionesContratoRepository transacciones)
    {
        _sesiones = sesiones;
        _transacciones = transacciones;
    }

    public async Task<SesionCerradaDto> Handle(CerrarSesionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoEmpresa))
            throw new DomainValidationException("Estimado usuario, no ha seleccionado la empresa cuya caja desea cerrar.");

        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrEmpty(request.Clave)
            || !await _transacciones.ValidarCredencialCajaAsync(request.UsuarioConectado, request.Usuario.Trim(), request.Clave, cancellationToken))
            throw new DomainValidationException(
                "Estimado usuario, el usuario o la contraseña no son válidos, o el usuario no tiene permiso para cerrar la caja.");

        return await _sesiones.CerrarSesionAsync(request.CodigoEmpresa, request.Usuario, cancellationToken);
    }
}
