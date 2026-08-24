using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Empenios;
using GoldmoneyBackend.Api.Mappings;
using GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratosByCedula;
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

    public EmpeniosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("contratos/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ContratoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratoById(string id, CancellationToken cancellationToken)
    {
        var contrato = await _mediator.Send(new GetContratoByIdQuery(id), cancellationToken);
        return Ok(contrato);
    }

    [HttpGet("clientes/{cedula}/contratos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ContratoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratosByCedula(string cedula, CancellationToken cancellationToken)
    {
        var contratos = await _mediator.Send(new GetContratosByCedulaQuery(cedula), cancellationToken);
        return Ok(contratos);
    }

    [HttpPost("contratos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(CreateContratoResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateContrato([FromBody] CreateEmpenioContratoRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var created = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetContratoById), new { id = created.ContratoId }, created);
    }
}