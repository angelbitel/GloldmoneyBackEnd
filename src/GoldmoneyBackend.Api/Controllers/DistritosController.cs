using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Distritos;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/distritos")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class DistritosController : ControllerBase
{
    private readonly IDistritosDataService _distritosDataService;

    public DistritosController(IDistritosDataService distritosDataService)
    {
        _distritosDataService = distritosDataService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DistritoDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _distritosDataService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoDistrito}")]
    [ProducesResponseType(typeof(DistritoDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoDistrito, CancellationToken cancellationToken)
    {
        var distrito = await _distritosDataService.GetByKeyAsync(codigoDistrito, cancellationToken);
        return distrito is null ? NotFound() : Ok(distrito);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(DistritoDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDistritoRequest request, CancellationToken cancellationToken)
    {
        var dto = new DistritoDbUpsertDto(request.CodigoDistrito, request.CodigoProvincia, request.NombreDistrito, request.Activo);
        await _distritosDataService.CreateAsync(dto, cancellationToken);
        var created = await _distritosDataService.GetByKeyAsync(request.CodigoDistrito, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoDistrito = request.CodigoDistrito }, created);
    }

    [HttpPut("{codigoDistrito}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(DistritoDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string codigoDistrito, [FromBody] UpdateDistritoRequest request, CancellationToken cancellationToken)
    {
        var dto = new DistritoDbUpsertDto(codigoDistrito, request.CodigoProvincia, request.NombreDistrito, request.Activo);
        await _distritosDataService.UpdateAsync(codigoDistrito, dto, cancellationToken);
        var updated = await _distritosDataService.GetByKeyAsync(codigoDistrito, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoDistrito}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoDistrito, CancellationToken cancellationToken)
    {
        await _distritosDataService.DeleteAsync(codigoDistrito, cancellationToken);
        return NoContent();
    }
}
