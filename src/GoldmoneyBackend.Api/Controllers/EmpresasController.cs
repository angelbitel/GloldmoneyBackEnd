using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Empresas;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/empresas")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class EmpresasController : ControllerBase
{
    private readonly IEmpresasRepository _empresasRepository;

    public EmpresasController(IEmpresasRepository empresasRepository)
    {
        _empresasRepository = empresasRepository;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEmpresaRequest request, CancellationToken cancellationToken)
    {
        var dto = new EmpresaDbUpsertDto(
            request.CodigoEmpresa,
            request.NombreEmpresa,
            request.Direccion,
            request.Ruc,
            request.Telefono,
            request.MontoInicial,
            request.MontoAuxiliar,
            request.ManejoCajaDep,
            request.CodEmpresaCaja);

        await _empresasRepository.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{codigoEmpresa}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(string codigoEmpresa, [FromBody] UpdateEmpresaRequest request, CancellationToken cancellationToken)
    {
        var dto = new EmpresaDbUpsertDto(
            codigoEmpresa,
            request.NombreEmpresa,
            request.Direccion,
            request.Ruc,
            request.Telefono,
            request.MontoInicial,
            request.MontoAuxiliar,
            request.ManejoCajaDep,
            request.CodEmpresaCaja);

        await _empresasRepository.UpdateAsync(dto, cancellationToken);
        return NoContent();
    }
}
