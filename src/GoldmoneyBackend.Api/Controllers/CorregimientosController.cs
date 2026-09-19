using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Corregimientos;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/corregimientos")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class CorregimientosController : ControllerBase
{
    private readonly ICorregimientosRepository _corregimientosRepository;

    public CorregimientosController(ICorregimientosRepository corregimientosRepository)
    {
        _corregimientosRepository = corregimientosRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CorregimientoDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _corregimientosRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoCorregimiento}")]
    [ProducesResponseType(typeof(CorregimientoDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoCorregimiento, CancellationToken cancellationToken)
    {
        var corregimiento = await _corregimientosRepository.GetByKeyAsync(codigoCorregimiento, cancellationToken);
        return corregimiento is null ? NotFound() : Ok(corregimiento);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(CorregimientoDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCorregimientoRequest request, CancellationToken cancellationToken)
    {
        var dto = new CorregimientoDbUpsertDto(request.CodigoCorregimiento, request.CodigoDistrito, request.NombreCorregimiento, request.Activo);
        await _corregimientosRepository.CreateAsync(dto, cancellationToken);
        var created = await _corregimientosRepository.GetByKeyAsync(request.CodigoCorregimiento, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoCorregimiento = request.CodigoCorregimiento }, created);
    }

    [HttpPut("{codigoCorregimiento}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(CorregimientoDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string codigoCorregimiento, [FromBody] UpdateCorregimientoRequest request, CancellationToken cancellationToken)
    {
        var dto = new CorregimientoDbUpsertDto(codigoCorregimiento, request.CodigoDistrito, request.NombreCorregimiento, request.Activo);
        await _corregimientosRepository.UpdateAsync(codigoCorregimiento, dto, cancellationToken);
        var updated = await _corregimientosRepository.GetByKeyAsync(codigoCorregimiento, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoCorregimiento}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoCorregimiento, CancellationToken cancellationToken)
    {
        await _corregimientosRepository.DeleteAsync(codigoCorregimiento, cancellationToken);
        return NoContent();
    }
}
