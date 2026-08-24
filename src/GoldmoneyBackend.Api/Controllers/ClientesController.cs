using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Clientes.Commands.CreateCliente;
using GoldmoneyBackend.Application.Clientes.Commands.DeleteCliente;
using GoldmoneyBackend.Application.Clientes.Commands.UpdateCliente;
using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Clientes.Queries.GetClienteById;
using GoldmoneyBackend.Application.Clientes.Queries.GetClientes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateClienteCommand command, CancellationToken cancellationToken)
    {
        var cliente = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await _mediator.Send(new GetClienteByIdQuery(id), cancellationToken);
        return Ok(cliente);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var clientes = await _mediator.Send(new GetClientesQuery(), cancellationToken);
        return Ok(clientes);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClienteCommand command, CancellationToken cancellationToken)
    {
        var request = command with { Id = id };
        var cliente = await _mediator.Send(request, cancellationToken);
        return Ok(cliente);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteClienteCommand(id), cancellationToken);
        return NoContent();
    }
}