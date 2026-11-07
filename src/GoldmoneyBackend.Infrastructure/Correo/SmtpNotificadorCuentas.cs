using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Infrastructure.Correo;

public sealed class SmtpNotificadorCuentas : INotificadorCuentas
{
    private readonly CorreoOptions _options;
    private readonly ILogger<SmtpNotificadorCuentas> _logger;

    public SmtpNotificadorCuentas(IOptions<CorreoOptions> options, ILogger<SmtpNotificadorCuentas> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnviarEnlaceRestablecimientoAsync(
        string correo,
        string nombreUsuario,
        string token,
        DateTime expiraEnUtc,
        CancellationToken cancellationToken)
    {
        var enlace = $"{_options.FrontendBaseUrl.TrimEnd('/')}/restablecer-contrasena?token={Uri.EscapeDataString(token)}";
        var minutos = Math.Max(1, (int)Math.Round((expiraEnUtc - DateTime.UtcNow).TotalMinutes));

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            if (_options.RegistrarEnlaceEnLog)
            {
                _logger.LogWarning("SMTP no configurado. Enlace de restablecimiento para {NombreUsuario}: {Enlace}", nombreUsuario, enlace);
            }
            else
            {
                _logger.LogError("SMTP no configurado (sección {Seccion}): no se envió el enlace de restablecimiento a {NombreUsuario}.", CorreoOptions.SectionName, nombreUsuario);
            }

            return;
        }

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

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName),
            Subject = "Restablecer tu contraseña de GoldMoney",
            Body = texto
        };
        mensaje.To.Add(correo);
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

        using var cliente = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? null
                : new NetworkCredential(_options.UserName, _options.Password)
        };

        try
        {
            await cliente.SendMailAsync(mensaje, cancellationToken);
        }
        catch (SmtpException ex)
        {
            // No se propaga: la respuesta al usuario debe ser la misma haya o no cuenta.
            _logger.LogError(ex, "No se pudo enviar el enlace de restablecimiento a {NombreUsuario}.", nombreUsuario);
        }
    }
}
