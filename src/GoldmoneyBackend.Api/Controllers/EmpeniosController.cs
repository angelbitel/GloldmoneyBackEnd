using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Empenios;
using GoldmoneyBackend.Api.Mappings;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratosByCedula;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/empenios")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class EmpeniosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IContratoNumeracionRepository _contratoNumeracionRepository;

    public EmpeniosController(IMediator mediator, IContratoNumeracionRepository contratoNumeracionRepository)
    {
        _mediator = mediator;
        _contratoNumeracionRepository = contratoNumeracionRepository;
    }

    [HttpGet("contratos/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ContratoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratoById(string id, CancellationToken cancellationToken)
    {
        var contrato = await _mediator.Send(new GetContratoByIdQuery(id), cancellationToken);
        return Ok(contrato);
    }

    [HttpGet("contratos/{id}/completo")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ContratoCompletoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratoCompleto(string id, CancellationToken cancellationToken)
    {
        var contrato = await _mediator.Send(new GetContratoByIdQuery(id), cancellationToken);
        if (contrato is null) return NotFound();

        var completo = await _mediator.Send(new GetContratoCompletoQuery(id), cancellationToken);
        return Ok(completo);
    }

    [HttpGet("clientes/{cedula}/contratos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ContratoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratosByCedula(string cedula, CancellationToken cancellationToken)
    {
        var contratos = await _mediator.Send(new GetContratosByCedulaQuery(cedula), cancellationToken);
        return Ok(contratos);
    }

    /// <summary>
    /// Número que probablemente tendrá el próximo contrato del grupo, solo para mostrarlo; no
    /// reserva nada. El número definitivo lo asigna <see cref="CreateContrato"/>.
    /// </summary>
    [HttpGet("contratos/proximo/{codigoEmpresa}/{codigoGrupo:int}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(ProximoContratoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProximoNumeroContrato(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var proximo = await _contratoNumeracionRepository.GetProximoAsync(codigoEmpresa, codigoGrupo, cancellationToken);
        return Ok(proximo);
    }

    /// <summary>
    /// Crea el empeño con la fecha de la sesión abierta, la tasa y el plazo del grupo y el
    /// siguiente número de su secuencia. Devuelve el número y las fechas para imprimir el contrato.
    /// </summary>
    [HttpPost("contratos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(CreateContratoResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateContrato([FromBody] CreateEmpenioContratoRequest request, CancellationToken cancellationToken)
    {
        if (User.UsuarioResponsable() is not { } usuarioResponsable)
        {
            return Unauthorized();
        }

        var command = request.ToCommand(usuarioResponsable);
        var created = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetContratoById), new { id = created.ContratoId }, created);
    }

    // ---- Administración de empeño (SADE: observación, copia de recibo, anulación) ----

    [HttpGet("clientes/{cedula}/contratos-transaccion")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ContratoTransaccionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratosTransaccion(string cedula, CancellationToken cancellationToken)
    {
        var contratos = await _mediator.Send(new GetContratosTransaccionQuery(cedula), cancellationToken);
        return Ok(contratos);
    }

    [HttpPut("contratos/{codigoEmpresa}/{codigoGrupo:int}/{numeroContrato}/observacion")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarObservacion(
        string codigoEmpresa, int codigoGrupo, string numeroContrato,
        [FromBody] ActualizarObservacionRequest request, CancellationToken cancellationToken)
    {
        var clave = new ContratoClave(codigoEmpresa, codigoGrupo, numeroContrato);
        var observacion = await _mediator.Send(new ActualizarObservacionCommand(clave, request.Observacion), cancellationToken);
        return Ok(new { observacion });
    }

    [HttpPost("contratos/{codigoEmpresa}/{codigoGrupo:int}/{numeroContrato}/copia-recibo")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(CopiaReciboResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerarCopiaRecibo(
        string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual();
        if (usuario is null) return Unauthorized();

        var clave = new ContratoClave(codigoEmpresa, codigoGrupo, numeroContrato);
        var resultado = await _mediator.Send(new GenerarCopiaReciboCommand(clave, usuario), cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("contratos/{codigoEmpresa}/{codigoGrupo:int}/{numeroContrato}/anular")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(AnularContratoResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnularContrato(
        string codigoEmpresa, int codigoGrupo, string numeroContrato,
        [FromBody] AnularContratoRequest? request, CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual();
        if (usuario is null) return Unauthorized();

        var clave = new ContratoClave(codigoEmpresa, codigoGrupo, numeroContrato);
        var command = new AnularContratoCommand(clave, usuario, request?.SupervisorUsuario, request?.SupervisorClave);
        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }

    private string? UsuarioActual() =>
        User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name);
}