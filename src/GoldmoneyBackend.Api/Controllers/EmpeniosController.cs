using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Empenios;
using GoldmoneyBackend.Api.Mappings;
using GoldmoneyBackend.Application.Common.Interfaces;
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

    [HttpGet("contratos/proximo/{codigoEmpresa}/{codigoGrupo:int}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(ProximoContratoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProximoNumeroContrato(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var proximo = await _contratoNumeracionRepository.GetProximoAsync(codigoEmpresa, codigoGrupo, cancellationToken);
        return Ok(proximo);
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