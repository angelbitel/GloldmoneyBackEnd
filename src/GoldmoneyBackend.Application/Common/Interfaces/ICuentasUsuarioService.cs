namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Correos de los usuarios de la tabla legacy USUARIOS y enlaces para restablecer su contraseña.
/// Un mismo nombre de usuario puede existir en varios módulos; el correo y la contraseña
/// se manejan por nombre de usuario, en todos sus módulos.
/// </summary>
public interface ICuentasUsuarioService
{
    /// <summary>Correos registrados, por nombre de usuario.</summary>
    Task<IReadOnlyDictionary<string, string>> GetCorreosAsync(CancellationToken cancellationToken);

    Task<bool> UsuarioExisteAsync(string nombreUsuario, CancellationToken cancellationToken);

    /// <summary>Guarda el correo del usuario, o lo elimina si <paramref name="correo"/> es null.</summary>
    Task GuardarCorreoAsync(string nombreUsuario, string? correo, CancellationToken cancellationToken);

    /// <summary>Usuario con correo registrado que coincide con el nombre de usuario o el correo indicado.</summary>
    Task<UsuarioCorreoDto?> BuscarPorUsuarioOCorreoAsync(string usuarioOCorreo, CancellationToken cancellationToken);

    Task<int> ContarSolicitudesDesdeAsync(string nombreUsuario, DateTime desdeUtc, CancellationToken cancellationToken);

    /// <summary>Registra un enlace nuevo e invalida los anteriores del usuario que no se hayan usado.</summary>
    Task RegistrarSolicitudAsync(string nombreUsuario, string tokenHash, DateTime creadoEnUtc, DateTime expiraEnUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Marca el enlace como usado y guarda la contraseña del sistema nuevo, en una sola transacción.
    /// Devuelve false si el enlace no existe, ya se usó, venció o el usuario ya no existe.
    /// La contraseña del VB6 (USUARIOS.contrasena) no cambia.
    /// </summary>
    Task<bool> RestablecerContrasenaAsync(string tokenHash, string nuevaContrasena, DateTime ahoraUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda la contraseña del sistema nuevo del usuario e invalida sus enlaces de restablecimiento
    /// pendientes. Devuelve false si el usuario no existe. La contraseña del VB6 no cambia.
    /// </summary>
    Task<bool> CambiarContrasenaAsync(string nombreUsuario, string nuevaContrasena, DateTime ahoraUtc, CancellationToken cancellationToken);
}

public sealed record UsuarioCorreoDto(string NombreUsuario, string Correo);
