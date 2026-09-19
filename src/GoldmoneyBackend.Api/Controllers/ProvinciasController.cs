using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Provincias;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/provincias")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ProvinciasController : ControllerBase
{
    private readonly IProvinciasRepository _provinciasRepository;

    public ProvinciasController(IProvinciasRepository provinciasRepository)
    {
        _provinciasRepository = provinciasRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProvinciaDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _provinciasRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoProvincia}")]
    [ProducesResponseType(typeof(ProvinciaDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoProvincia, CancellationToken cancellationToken)
    {
        var provincia = await _provinciasRepository.GetByKeyAsync(codigoProvincia, cancellationToken);
        return provincia is null ? NotFound() : Ok(provincia);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ProvinciaDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProvinciaRequest request, CancellationToken cancellationToken)
    {
        var dto = new ProvinciaDbUpsertDto(request.CodigoProvincia, request.NombreProvincia, request.Activo);
        await _provinciasRepository.CreateAsync(dto, cancellationToken);
        var created = await _provinciasRepository.GetByKeyAsync(request.CodigoProvincia, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoProvincia = request.CodigoProvincia }, created);
    }

    [HttpPut("{codigoProvincia}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ProvinciaDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string codigoProvincia, [FromBody] UpdateProvinciaRequest request, CancellationToken cancellationToken)
    {
        var dto = new ProvinciaDbUpsertDto(codigoProvincia, request.NombreProvincia, request.Activo);
        await _provinciasRepository.UpdateAsync(codigoProvincia, dto, cancellationToken);
        var updated = await _provinciasRepository.GetByKeyAsync(codigoProvincia, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoProvincia}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoProvincia, CancellationToken cancellationToken)
    {
        await _provinciasRepository.DeleteAsync(codigoProvincia, cancellationToken);
        return NoContent();
    }
}
