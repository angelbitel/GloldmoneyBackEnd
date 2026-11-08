using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.ClientesDb;
using GoldmoneyBackend.Application.Clientes.Commands.ActualizarComentarioCliente;
using GoldmoneyBackend.Application.Clientes.Commands.CreateCliente;
using GoldmoneyBackend.Application.Clientes.Commands.DeleteCliente;
using GoldmoneyBackend.Application.Clientes.Commands.UpdateCliente;
using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
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
    private readonly IClientesRepository _clientesRepository;

    public ClientesController(IMediator mediator, IClientesRepository clientesRepository)
    {
        _mediator = mediator;
        _clientesRepository = clientesRepository;
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
    [ProducesResponseType(typeof(IReadOnlyList<ClienteDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var clientes = await _clientesRepository.GetAllAsync(search, cancellationToken);
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

    /// <summary>Guarda la nota del cliente (CLIENTES.comentario, compartida con el VB6). Vacía la borra.</summary>
    [HttpPut("{idCliente}/comentario")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActualizarComentario(string idCliente, [FromBody] ActualizarComentarioClienteRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActualizarComentarioClienteCommand(idCliente, request.Comentario), cancellationToken);
        return NoContent();
    }
}