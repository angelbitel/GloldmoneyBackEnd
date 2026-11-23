using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.ContinuarSesion;

/// <summary>
/// Entrar a trabajar en la sesión de caja que ya está abierta (por ejemplo, retomarla otro día).
/// Solo valida la contraseña y que la sesión siga abierta; no modifica datos.
/// </summary>
public sealed record ContinuarSesionCommand(string CodigoEmpresa, string Usuario, string Clave, string UsuarioConectado)
    : IRequest<SesionOperacionDto>;

public sealed class ContinuarSesionCommandHandler : IRequestHandler<ContinuarSesionCommand, SesionOperacionDto>
{
    private readonly ITransaccionesContratoRepository _transacciones;

    public ContinuarSesionCommandHandler(ITransaccionesContratoRepository transacciones)
    {
        _transacciones = transacciones;
    }

    public async Task<SesionOperacionDto> Handle(ContinuarSesionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoEmpresa))
            throw new DomainValidationException("Estimado usuario, no ha seleccionado la caja con la que desea trabajar.");

        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrEmpty(request.Clave)
            || !await _transacciones.ValidarCredencialCajaAsync(request.UsuarioConectado, request.Usuario.Trim(), request.Clave, cancellationToken))
            throw new DomainValidationException(
                "Estimado usuario, el usuario o la contraseña no son válidos, o el usuario no tiene permiso para usar esta sesión de caja.");

        return await _transacciones.GetSesionOperacionAsync(request.CodigoEmpresa.Trim(), cancellationToken)
            ?? throw new DomainValidationException("Estimado usuario, la caja seleccionada ya no tiene una sesión abierta.");
    }
}
