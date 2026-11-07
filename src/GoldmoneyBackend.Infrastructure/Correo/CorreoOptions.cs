namespace GoldmoneyBackend.Infrastructure.Correo;

/// <summary>Configuración del envío de correos (sección "Correo").</summary>
public sealed class CorreoOptions
{
    public const string SectionName = "Correo";

    /// <summary>Servidor SMTP. Vacío = no se envían correos.</summary>
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "GoldMoney";

    /// <summary>URL del frontend donde está la pantalla /restablecer-contrasena.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Solo para desarrollo: si no hay SMTP, escribe el enlace en el log para poder probar el flujo.
    /// </summary>
    public bool RegistrarEnlaceEnLog { get; set; }
}
