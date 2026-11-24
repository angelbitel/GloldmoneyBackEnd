using System.Net;

namespace GoldmoneyBackend.Infrastructure.Correo;

/// <summary>Contenido del correo de restablecimiento, igual para cualquier proveedor.</summary>
internal sealed record MensajeRestablecimiento(string Enlace, string Asunto, string Texto, string Html)
{
    public static MensajeRestablecimiento Crear(CorreoOptions options, string nombreUsuario, string token, DateTime expiraEnUtc)
    {
        var enlace = $"{options.FrontendBaseUrl.TrimEnd('/')}/restablecer-contrasena?token={Uri.EscapeDataString(token)}";
        var minutos = Math.Max(1, (int)Math.Round((expiraEnUtc - DateTime.UtcNow).TotalMinutes));
        var usuario = WebUtility.HtmlEncode(nombreUsuario);

        var texto = $"""
            Hola {nombreUsuario}:

            Recibimos una solicitud para restablecer la contraseña de tu usuario en GoldMoney.
            Para elegir una nueva, abre este enlace (vence en {minutos} minutos y solo se puede usar una vez):

            {enlace}

            Si no fuiste tú, ignora este correo: tu contraseña no cambiará.
            """;
        var html = $"""
            <p>Hola <strong>{usuario}</strong>:</p>
            <p>Recibimos una solicitud para restablecer la contraseña de tu usuario en GoldMoney.</p>
            <p><a href="{WebUtility.HtmlEncode(enlace)}">Elegir una contraseña nueva</a></p>
            <p>El enlace vence en {minutos} minutos y solo se puede usar una vez.</p>
            <p>Si no fuiste tú, ignora este correo: tu contraseña no cambiará.</p>
            """;

        return new MensajeRestablecimiento(enlace, "Restablecer tu contraseña de GoldMoney", texto, html);
    }
}
