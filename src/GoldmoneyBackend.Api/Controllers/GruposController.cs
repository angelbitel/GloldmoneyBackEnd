using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Grupos;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class GruposController : ControllerBase
{
    private readonly IGruposDataService _gruposDataService;

    public GruposController(IGruposDataService gruposDataService)
    {
        _gruposDataService = gruposDataService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GrupoDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _gruposDataService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoEmpresa}/{codigoGrupo:int}")]
    [ProducesResponseType(typeof(GrupoDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var grupo = await _gruposDataService.GetByKeyAsync(codigoEmpresa, codigoGrupo, cancellationToken);
        return grupo is null ? NotFound() : Ok(grupo);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(GrupoDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateGrupoRequest request, CancellationToken cancellationToken)
    {
        var dto = new GrupoDbUpsertDto(
            request.CodigoEmpresa,
            request.CodigoGrupo,
            request.AbreviaturaGrupo,
            request.FechaCreacion,
            request.DescripcionGrupo,
            request.TasaInteres,
            request.MesesPlazo,
            request.EstatusSerie,
            request.SerieInicial,
            request.EstadoGrupo,
            request.CaracteristicaGrupo,
            request.BloquearInteres,
            request.BloquearPlazo,
            request.TasaInteresNocturna);

        await _gruposDataService.CreateAsync(dto, cancellationToken);
        var created = await _gruposDataService.GetByKeyAsync(request.CodigoEmpresa, request.CodigoGrupo, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoEmpresa = request.CodigoEmpresa, codigoGrupo = request.CodigoGrupo }, created);
    }

    [HttpPut("{codigoEmpresa}/{codigoGrupo:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(GrupoDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        string codigoEmpresa,
        int codigoGrupo,
        [FromBody] UpdateGrupoRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new GrupoDbUpsertDto(
            codigoEmpresa,
            codigoGrupo,
            request.AbreviaturaGrupo,
            request.FechaCreacion,
            request.DescripcionGrupo,
            request.TasaInteres,
            request.MesesPlazo,
            request.EstatusSerie,
            request.SerieInicial,
            request.EstadoGrupo,
            request.CaracteristicaGrupo,
            request.BloquearInteres,
            request.BloquearPlazo,
            request.TasaInteresNocturna);

        await _gruposDataService.UpdateAsync(codigoEmpresa, codigoGrupo, dto, cancellationToken);
        var updated = await _gruposDataService.GetByKeyAsync(codigoEmpresa, codigoGrupo, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoEmpresa}/{codigoGrupo:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        await _gruposDataService.DeleteAsync(codigoEmpresa, codigoGrupo, cancellationToken);
        return NoContent();
    }
}
