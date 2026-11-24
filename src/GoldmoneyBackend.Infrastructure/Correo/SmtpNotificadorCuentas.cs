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
        var contenido = MensajeRestablecimiento.Crear(_options, nombreUsuario, token, expiraEnUtc);
        var enlace = contenido.Enlace;

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

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName),
            Subject = contenido.Asunto,
            Body = contenido.Texto
        };
        mensaje.To.Add(correo);
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(contenido.Html, null, MediaTypeNames.Text.Html));

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
