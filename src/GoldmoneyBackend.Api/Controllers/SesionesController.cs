using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Sesiones;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;
using GoldmoneyBackend.Application.Sesiones.Queries.GetEstadoSesiones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/sesiones")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class SesionesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SesionesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("empresas-pendientes")]
    [ProducesResponseType(typeof(IReadOnlyList<EstadoSesionEmpresaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmpresasPendientes(CancellationToken cancellationToken)
    {
        var estados = await _mediator.Send(new GetEstadoSesionesQuery(), cancellationToken);
        return Ok(estados);
    }

    /// <summary>
    /// Abre la caja de la empresa para hoy (fecha local), como el VB6, con el PIN del usuario.
    /// Responde 400 si el PIN falta o no es correcto, 409 si la empresa ya tiene una caja abierta
    /// o ya tuvo sesión hoy y 429 tras varios intentos seguidos.
    /// </summary>
    [HttpPost("abrir")]
    [EnableRateLimiting(RateLimitPolicies.VerificacionPin)]
    [ProducesResponseType(typeof(SesionAbiertaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Abrir([FromBody] AbrirSesionRequest request, CancellationToken cancellationToken)
    {
        if (User.UsuarioResponsable() is not { } usuarioResponsable)
        {
            return Unauthorized();
        }

        var command = new AbrirSesionCommand(request.CodigoEmpresa, usuarioResponsable, request.Pin ?? string.Empty);
        var sesion = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, sesion);
    }
}
