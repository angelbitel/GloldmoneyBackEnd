using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Api.Contracts.ClientesDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/clientes-db")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ClientesDbController : ControllerBase
{
    private readonly IClientesDataService _clientesDataService;

    public ClientesDbController(IClientesDataService clientesDataService)
    {
        _clientesDataService = clientesDataService;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateClienteDbRequest request, CancellationToken cancellationToken)
    {
        var dto = new ClienteDbUpsertDto(
            request.IdCliente,
            request.Apellido,
            request.Nombre,
            request.Telefono,
            request.Estatus,
            request.Direccion,
            request.Comentario,
            request.CodigoPais,
            request.CodigoProvincia,
            request.CodigoDistrito,
            request.CodigoCorregimiento);

        await _clientesDataService.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{idCliente}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(string idCliente, [FromBody] UpdateClienteDbRequest request, CancellationToken cancellationToken)
    {
        var dto = new ClienteDbUpsertDto(
            idCliente,
            request.Apellido,
            request.Nombre,
            request.Telefono,
            request.Estatus,
            request.Direccion,
            request.Comentario,
            request.CodigoPais,
            request.CodigoProvincia,
            request.CodigoDistrito,
            request.CodigoCorregimiento);

        await _clientesDataService.UpdateAsync(dto, cancellationToken);
        return NoContent();
    }
}
