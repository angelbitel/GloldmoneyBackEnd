using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

/// <summary>Inventario de prendas en custodia: contratos vigentes (CONTRATOS) con el resumen de sus prendas.</summary>
[ApiController]
[Route("api/inventario")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class InventarioController : ControllerBase
{
    private readonly IAdministracionReadService _readService;

    public InventarioController(IAdministracionReadService readService)
    {
        _readService = readService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(InventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string codigoEmpresa = "1",
        [FromQuery] int? codigoGrupo = null,
        [FromQuery] string? buscar = null,
        CancellationToken cancellationToken = default)
    {
        var inventario = await _readService.GetInventarioAsync(codigoEmpresa, codigoGrupo, buscar, cancellationToken);
        return Ok(inventario);
    }
}
