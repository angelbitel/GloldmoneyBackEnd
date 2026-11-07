using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GoldmoneyBackend.Application.Auth.Commands.SolicitarRestablecimientoContrasena;

public sealed class SolicitarRestablecimientoContrasenaCommandHandler : IRequestHandler<SolicitarRestablecimientoContrasenaCommand>
{
    private const int VigenciaMinutos = 30;
    private const int MaximoSolicitudesPorHora = 3;

    private readonly ICuentasUsuarioService _cuentas;
    private readonly INotificadorCuentas _notificador;
    private readonly ILogger<SolicitarRestablecimientoContrasenaCommandHandler> _logger;

    public SolicitarRestablecimientoContrasenaCommandHandler(
        ICuentasUsuarioService cuentas,
        INotificadorCuentas notificador,
        ILogger<SolicitarRestablecimientoContrasenaCommandHandler> logger)
    {
        _cuentas = cuentas;
        _notificador = notificador;
        _logger = logger;
    }

    public async Task Handle(SolicitarRestablecimientoContrasenaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await _cuentas.BuscarPorUsuarioOCorreoAsync(request.UsuarioOCorreo, cancellationToken);
        if (cuenta is null)
        {
            _logger.LogInformation("Solicitud de restablecimiento sin cuenta con correo registrado.");
            return;
        }

        var ahora = DateTime.UtcNow;
        var recientes = await _cuentas.ContarSolicitudesDesdeAsync(cuenta.NombreUsuario, ahora.AddHours(-1), cancellationToken);
        if (recientes >= MaximoSolicitudesPorHora)
        {
            _logger.LogWarning("Se alcanzó el límite de solicitudes de restablecimiento para {NombreUsuario}.", cuenta.NombreUsuario);
            return;
        }

        var token = TokenRestablecimiento.Generar();
        var expira = ahora.AddMinutes(VigenciaMinutos);

        await _cuentas.RegistrarSolicitudAsync(cuenta.NombreUsuario, TokenRestablecimiento.Hash(token), ahora, expira, cancellationToken);
        await _notificador.EnviarEnlaceRestablecimientoAsync(cuenta.Correo, cuenta.NombreUsuario, token, expira, cancellationToken);
    }
}
