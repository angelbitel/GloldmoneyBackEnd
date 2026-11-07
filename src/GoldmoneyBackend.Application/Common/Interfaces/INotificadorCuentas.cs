namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>Envía al usuario los correos relacionados con su cuenta.</summary>
public interface INotificadorCuentas
{
    /// <summary>Envía el enlace para restablecer la contraseña.</summary>
    Task EnviarEnlaceRestablecimientoAsync(
        string correo,
        string nombreUsuario,
        string token,
        DateTime expiraEnUtc,
        CancellationToken cancellationToken);
}
