using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Sesiones;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;
using GoldmoneyBackend.Application.Sesiones.Queries.GetEstadoSesiones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("abrir")]
    [ProducesResponseType(typeof(SesionAbiertaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Abrir([FromBody] AbrirSesionRequest request, CancellationToken cancellationToken)
    {
        var usuarioResponsable = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(usuarioResponsable))
        {
            return Unauthorized();
        }

        var command = new AbrirSesionCommand(request.CodigoEmpresa, request.FechaApertura, usuarioResponsable);
        var sesion = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, sesion);
    }
}
