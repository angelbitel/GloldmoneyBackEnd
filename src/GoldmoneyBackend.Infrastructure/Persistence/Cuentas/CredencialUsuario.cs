namespace GoldmoneyBackend.Infrastructure.Persistence.Cuentas;

/// <summary>
/// Contraseña de un usuario de USUARIOS para el sistema nuevo, como hash (ver HasherContrasenas).
/// Va aparte porque USUARIOS.contrasena usa el cifrado del VB6, que sigue leyéndola: cada sistema
/// tiene su propia contraseña.
/// </summary>
public sealed class CredencialUsuario
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime ActualizadoEnUtc { get; set; }
}
