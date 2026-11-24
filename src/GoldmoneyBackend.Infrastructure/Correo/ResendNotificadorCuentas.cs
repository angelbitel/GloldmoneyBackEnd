using System.Net.Http.Headers;
using System.Net.Http.Json;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Infrastructure.Correo;

/// <summary>
/// Envía los correos con la API HTTP de Resend (plan gratis: 3.000 al mes, 100 al día). Se usa en
/// lugar de SMTP porque Render bloquea los puertos SMTP salientes en algunos planes.
/// </summary>
public sealed class ResendNotificadorCuentas : INotificadorCuentas
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.resend.com/"),
        Timeout = TimeSpan.FromSeconds(20)
    };

    private readonly CorreoOptions _options;
    private readonly ILogger<ResendNotificadorCuentas> _logger;

    public ResendNotificadorCuentas(IOptions<CorreoOptions> options, ILogger<ResendNotificadorCuentas> logger)
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from = $"{_options.FromName} <{_options.From}>",
                to = new[] { correo },
                subject = mensaje.Asunto,
                html = mensaje.Html,
                text = mensaje.Texto
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ResendApiKey);

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Enlace de restablecimiento enviado por Resend a {NombreUsuario}.", nombreUsuario);
                return;
            }

            var detalle = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Resend rechazó el correo de restablecimiento para {NombreUsuario}: {Status} {Detalle}",
                nombreUsuario, (int)response.StatusCode, detalle.Length > 500 ? detalle[..500] : detalle);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // No se propaga: la respuesta al usuario debe ser la misma haya o no cuenta.
            _logger.LogError(ex, "No se pudo contactar a Resend para enviar el enlace a {NombreUsuario}.", nombreUsuario);
        }
    }
}
