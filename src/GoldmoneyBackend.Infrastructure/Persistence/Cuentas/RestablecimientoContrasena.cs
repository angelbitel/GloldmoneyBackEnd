namespace GoldmoneyBackend.Infrastructure.Persistence.Cuentas;

/// <summary>
/// Enlace de un solo uso para restablecer la contraseña. Solo se guarda el hash
/// SHA-256 del token: el token en claro únicamente viaja en el correo.
/// </summary>
public sealed class RestablecimientoContrasena
{
    public Guid Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ExpiraEnUtc { get; set; }
    public DateTime? UsadoEnUtc { get; set; }
}
