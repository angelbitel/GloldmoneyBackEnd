using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.ClientesDb;
using GoldmoneyBackend.Application.Clientes.Commands.ActualizarComentarioCliente;
using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

/// <summary>
/// Clientes de la tabla legacy CLIENTES. La clave es la cédula (id_cliente), p. ej. 008-0481-00391.
/// No hay DELETE: los clientes quedan referenciados por CONTRATOS e HISTORIAL_CONTRATOS.
/// </summary>
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

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ClienteDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var clientes = await _clientesRepository.GetAllAsync(search, cancellationToken);
        return Ok(clientes);
    }

    /// <summary>Listado paginado de clientes con búsqueda (nombre o cédula), filtro y totales.</summary>
    [HttpGet("paginado")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ClientesPaginadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaginado(
        [FromQuery] string? buscar,
        [FromQuery] FiltroClientes filtro = FiltroClientes.Todos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = PaginacionClientes.TamanoPorDefecto,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _clientesRepository.GetPaginadoAsync(buscar, filtro, pagina, tamanoPagina, cancellationToken));
    }

    [HttpGet("{idCliente}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ClienteDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string idCliente, CancellationToken cancellationToken)
    {
        var cliente = await _clientesRepository.GetByIdAsync(idCliente, cancellationToken);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(ClienteDbDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClienteDbRequest request, CancellationToken cancellationToken)
    {
        var creado = await _clientesRepository.CreateAsync(
            new ClienteDbUpsertDto(
                request.IdCliente, request.Apellido, request.Nombre, request.Telefono, request.Estatus ?? 1,
                request.Direccion, request.Comentario, request.CodigoPais, request.CodigoProvincia,
                request.CodigoDistrito, request.CodigoCorregimiento),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { idCliente = creado.IdCliente }, creado);
    }

    [HttpPut("{idCliente}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(ClienteDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string idCliente, [FromBody] UpdateClienteDbRequest request, CancellationToken cancellationToken)
    {
        var actualizado = await _clientesRepository.UpdateAsync(
            new ClienteDbUpsertDto(
                idCliente, request.Apellido, request.Nombre, request.Telefono, request.Estatus ?? 1,
                request.Direccion, request.Comentario, request.CodigoPais, request.CodigoProvincia,
                request.CodigoDistrito, request.CodigoCorregimiento),
            cancellationToken);

        return Ok(actualizado);
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
