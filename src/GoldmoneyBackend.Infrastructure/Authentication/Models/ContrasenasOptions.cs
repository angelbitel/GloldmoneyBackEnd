namespace GoldmoneyBackend.Infrastructure.Authentication.Models;

/// <summary>Cifrado de las contraseñas propias del sistema nuevo (tabla UsuariosCredencial).</summary>
public sealed class ContrasenasOptions
{
    public const string SectionName = "Contrasenas";
    public const int IteracionesMinimas = 100_000;
    public const int LongitudMinimaPepper = 32;

    /// <summary>Iteraciones de PBKDF2-SHA256 para los hashes nuevos; los existentes se rehacen al iniciar sesión.</summary>
    public int Iteraciones { get; set; } = 600_000;

    /// <summary>
    /// Clave secreta que se mezcla con la contraseña (HMAC-SHA256) antes del hash; no se guarda en la base.
    /// Vacía = sin pepper. Si se cambia o se pierde, las contraseñas guardadas con la anterior dejan de servir.
    /// </summary>
    public string Pepper { get; set; } = string.Empty;
}
