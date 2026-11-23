using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Soporte;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

/// <summary>
/// Casos de soporte. Cualquier usuario abre y sigue los casos de su empresa; la bandeja, los
/// cambios de estado/prioridad/asignación y las notas internas son del equipo de soporte
/// (rol Admin o usuario_soporte / usuario_admin en USUARIOS).
/// </summary>
[ApiController]
[Route("api/soporte")]
[Authorize]
public sealed class SoporteController : ControllerBase
{
    /// <summary>5 archivos de 10 MB más el resto del formulario.</summary>
    private const long MaxSubida = 52 * 1024 * 1024;

    private readonly ISoporteService _soporte;

    public SoporteController(ISoporteService soporte)
    {
        _soporte = soporte;
    }

    public sealed record CrearTicketRequest(
        string CodigoEmpresa,
        string Asunto,
        string Categoria,
        string Prioridad,
        string Mensaje,
        string? NumeroContrato,
        string? Pantalla,
        string? Origen);

    public sealed record ResponderTicketRequest(string Mensaje, bool EsInterno, string? NuevoEstado);

    public sealed record ActualizarTicketRequest(string? Estado, string? Prioridad, string? UsuarioAsignado);

    /// <summary>Casos de una empresa (la que el usuario tiene seleccionada).</summary>
    [HttpGet("tickets")]
    [ProducesResponseType(typeof(TicketsPaginaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string codigoEmpresa,
        [FromQuery] string? estado,
        [FromQuery] bool incluirCerrados,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var filtro = new FiltroTickets(codigoEmpresa, estado, null, null, null, false, incluirCerrados, page, pageSize);
        return Ok(await _soporte.GetTicketsAsync(usuario, filtro, false, cancellationToken));
    }

    /// <summary>Bandeja del equipo de soporte: casos de todas las empresas.</summary>
    [HttpGet("bandeja")]
    [ProducesResponseType(typeof(TicketsPaginaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBandeja(
        [FromQuery] string? codigoEmpresa,
        [FromQuery] string? estado,
        [FromQuery] string? categoria,
        [FromQuery] string? prioridad,
        [FromQuery] string? texto,
        [FromQuery] bool soloMios,
        [FromQuery] bool incluirCerrados,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var filtro = new FiltroTickets(codigoEmpresa, estado, categoria, prioridad, texto, soloMios, incluirCerrados, page, pageSize);
        return Ok(await _soporte.GetTicketsAsync(usuario, filtro, true, cancellationToken));
    }

    /// <summary>Si el usuario es del equipo de soporte (para mostrar la bandeja en el menú).</summary>
    [HttpGet("yo")]
    public async Task<IActionResult> GetYo(CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        return Ok(new { esSoporte = await _soporte.EsSoporteAsync(usuario, cancellationToken) });
    }

    [HttpGet("tickets/{id:int}")]
    [ProducesResponseType(typeof(TicketDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(int id, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        return Ok(await _soporte.GetTicketAsync(usuario, id, cancellationToken));
    }

    [HttpPost("tickets")]
    [ProducesResponseType(typeof(TicketDetalleDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear([FromBody] CrearTicketRequest request, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var dto = new CrearTicketDto(
            request.CodigoEmpresa,
            request.Asunto,
            request.Categoria,
            request.Prioridad,
            request.Mensaje,
            request.NumeroContrato,
            request.Pantalla,
            request.Origen);
        var ticket = await _soporte.CrearAsync(usuario, dto, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { id = ticket.IdTicket }, ticket);
    }

    [HttpPost("tickets/{id:int}/mensajes")]
    [ProducesResponseType(typeof(TicketDetalleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Responder(int id, [FromBody] ResponderTicketRequest request, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var dto = new ResponderTicketDto(request.Mensaje, request.EsInterno, request.NuevoEstado);
        return Ok(await _soporte.ResponderAsync(usuario, id, dto, cancellationToken));
    }

    [HttpPost("tickets/{id:int}/cerrar")]
    [ProducesResponseType(typeof(TicketDetalleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cerrar(int id, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        return Ok(await _soporte.CerrarAsync(usuario, id, cancellationToken));
    }

    [HttpPut("tickets/{id:int}")]
    [ProducesResponseType(typeof(TicketDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarTicketRequest request, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var dto = new ActualizarTicketDto(request.Estado, request.Prioridad, request.UsuarioAsignado);
        return Ok(await _soporte.ActualizarAsync(usuario, id, dto, cancellationToken));
    }

    [HttpGet("agentes")]
    [ProducesResponseType(typeof(IReadOnlyList<AgenteSoporteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgentes(CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        if (!await _soporte.EsSoporteAsync(usuario, cancellationToken)) return Forbid();
        return Ok(await _soporte.GetAgentesAsync(cancellationToken));
    }

    /// <summary>Sube archivos a un mensaje propio (el idMensajeNuevo que devuelven crear y responder).</summary>
    [HttpPost("tickets/{id:int}/mensajes/{idMensaje:int}/adjuntos")]
    [RequestSizeLimit(MaxSubida)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxSubida)]
    [ProducesResponseType(typeof(TicketDetalleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubirAdjuntos(int id, int idMensaje, [FromForm] List<IFormFile> archivos, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var entrantes = archivos
            .Select(f => new AdjuntoEntrante(f.FileName, f.ContentType, f.Length, f.OpenReadStream))
            .ToList();
        return Ok(await _soporte.SubirAdjuntosAsync(usuario, id, idMensaje, entrantes, cancellationToken));
    }

    [HttpGet("tickets/{id:int}/adjuntos/{idAdjunto:int}")]
    public async Task<IActionResult> GetAdjunto(int id, int idAdjunto, CancellationToken cancellationToken)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        var archivo = await _soporte.GetAdjuntoAsync(usuario, id, idAdjunto, cancellationToken);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(archivo.Contenido, archivo.ContentType, archivo.Nombre);
    }

    private UsuarioSoporte? Usuario() =>
        User.UsuarioResponsable() is { } nombre ? new UsuarioSoporte(nombre, User.IsInRole("Admin")) : null;
}
