using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/contratos")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ContratosNumeracionController : ControllerBase
{
    private readonly IContratoNumeracionRepository _contratoNumeracionRepository;

    public ContratosNumeracionController(IContratoNumeracionRepository contratoNumeracionRepository)
    {
        _contratoNumeracionRepository = contratoNumeracionRepository;
    }

    [HttpGet("proximo/{codigoEmpresa}/{codigoGrupo:int}")]
    [ProducesResponseType(typeof(ProximoContratoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProximo(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var proximo = await _contratoNumeracionRepository.GetProximoAsync(codigoEmpresa, codigoGrupo, cancellationToken);
        return Ok(proximo);
    }
}