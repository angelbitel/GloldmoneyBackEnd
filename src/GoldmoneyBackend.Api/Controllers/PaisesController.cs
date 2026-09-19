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
    private readonly IPaisesRepository _paisesRepository;

    public PaisesController(IPaisesRepository paisesRepository)
    {
        _paisesRepository = paisesRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaisDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _paisesRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoPais}")]
    [ProducesResponseType(typeof(PaisDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoPais, CancellationToken cancellationToken)
    {
        var pais = await _paisesRepository.GetByKeyAsync(codigoPais, cancellationToken);
        return pais is null ? NotFound() : Ok(pais);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(PaisDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePaisRequest request, CancellationToken cancellationToken)
    {
        var dto = new PaisDbUpsertDto(request.CodigoPais, request.NombrePais, request.Activo);
        await _paisesRepository.CreateAsync(dto, cancellationToken);
        var created = await _paisesRepository.GetByKeyAsync(request.CodigoPais, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoPais = request.CodigoPais }, created);
    }

    [HttpPut("{codigoPais}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(PaisDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string codigoPais, [FromBody] UpdatePaisRequest request, CancellationToken cancellationToken)
    {
        var dto = new PaisDbUpsertDto(codigoPais, request.NombrePais, request.Activo);
        await _paisesRepository.UpdateAsync(codigoPais, dto, cancellationToken);
        var updated = await _paisesRepository.GetByKeyAsync(codigoPais, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoPais}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoPais, CancellationToken cancellationToken)
    {
        await _paisesRepository.DeleteAsync(codigoPais, cancellationToken);
        return NoContent();
    }
}
