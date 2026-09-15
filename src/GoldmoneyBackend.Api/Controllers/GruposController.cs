using GoldmoneyBackend.Api.Authorization;
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
}
