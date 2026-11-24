using System.Net.Http.Json;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Infrastructure.Correo;

/// <summary>
/// Envía los correos con la API HTTP de Brevo (plan gratis: 300 al día). Se usa en lugar de SMTP
/// porque Render bloquea los puertos SMTP salientes en algunos planes.
/// </summary>
public sealed class BrevoNotificadorCuentas : INotificadorCuentas
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.brevo.com/v3/"),
        Timeout = TimeSpan.FromSeconds(20)
    };

    private readonly CorreoOptions _options;
    private readonly ILogger<BrevoNotificadorCuentas> _logger;

    public BrevoNotificadorCuentas(IOptions<CorreoOptions> options, ILogger<BrevoNotificadorCuentas> logger)
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
        var mensaje = MensajeRestablecimiento.Crear(_options, nombreUsuario, token, expiraEnUtc);

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = _options.FromName, email = _options.From },
                to = new[] { new { email = correo, name = nombreUsuario } },
                subject = mensaje.Asunto,
                htmlContent = mensaje.Html,
                textContent = mensaje.Texto
            })
        };
        request.Headers.Add("api-key", _options.BrevoApiKey);
        request.Headers.Add("accept", "application/json");

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Enlace de restablecimiento enviado por Brevo a {NombreUsuario}.", nombreUsuario);
                return;
            }

            var detalle = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Brevo rechazó el correo de restablecimiento para {NombreUsuario}: {Status} {Detalle}",
                nombreUsuario, (int)response.StatusCode, detalle.Length > 500 ? detalle[..500] : detalle);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // No se propaga: la respuesta al usuario debe ser la misma haya o no cuenta.
            _logger.LogError(ex, "No se pudo contactar a Brevo para enviar el enlace a {NombreUsuario}.", nombreUsuario);
        }
    }
}
