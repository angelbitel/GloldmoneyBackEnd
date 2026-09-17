using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Paises;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/paises")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class PaisesController : ControllerBase
{
    private readonly IPaisesDataService _paisesDataService;

    public PaisesController(IPaisesDataService paisesDataService)
    {
        _paisesDataService = paisesDataService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaisDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _paisesDataService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoPais}")]
    [ProducesResponseType(typeof(PaisDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoPais, CancellationToken cancellationToken)
    {
        var pais = await _paisesDataService.GetByKeyAsync(codigoPais, cancellationToken);
        return pais is null ? NotFound() : Ok(pais);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(PaisDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePaisRequest request, CancellationToken cancellationToken)
    {
        var dto = new PaisDbUpsertDto(request.CodigoPais, request.NombrePais, request.Activo);
        await _paisesDataService.CreateAsync(dto, cancellationToken);
        var created = await _paisesDataService.GetByKeyAsync(request.CodigoPais, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoPais = request.CodigoPais }, created);
    }

    [HttpPut("{codigoPais}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(PaisDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string codigoPais, [FromBody] UpdatePaisRequest request, CancellationToken cancellationToken)
    {
        var dto = new PaisDbUpsertDto(codigoPais, request.NombrePais, request.Activo);
        await _paisesDataService.UpdateAsync(codigoPais, dto, cancellationToken);
        var updated = await _paisesDataService.GetByKeyAsync(codigoPais, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoPais}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoPais, CancellationToken cancellationToken)
    {
        await _paisesDataService.DeleteAsync(codigoPais, cancellationToken);
        return NoContent();
    }
}
