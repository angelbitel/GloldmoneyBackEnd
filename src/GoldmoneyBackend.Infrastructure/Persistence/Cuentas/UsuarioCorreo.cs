namespace GoldmoneyBackend.Infrastructure.Persistence.Cuentas;

/// <summary>
/// Correo de un usuario de la tabla legacy USUARIOS. Va en una tabla aparte para no
/// alterar USUARIOS, que el VB6 sigue leyendo y escribiendo.
/// </summary>
public sealed class UsuarioCorreo
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public DateTime ActualizadoEnUtc { get; set; }
}
