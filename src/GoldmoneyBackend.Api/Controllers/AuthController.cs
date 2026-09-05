using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Auth;
using GoldmoneyBackend.Application.Auth.Commands.Login;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Auth.Queries.GetUsuarios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _mediator.Send(new LoginCommand(request.UserName, request.Password), cancellationToken);
        return Ok(token);
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsuarios(CancellationToken cancellationToken)
    {
        var usuarios = await _mediator.Send(new GetUsuariosQuery(), cancellationToken);
        return Ok(usuarios);
    }
}
