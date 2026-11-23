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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmpresaDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _empresasRepository.GetAllAsync(cancellationToken));

    [HttpGet("{codigoEmpresa}")]
    [ProducesResponseType(typeof(EmpresaDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var empresa = await _empresasRepository.GetByIdAsync(codigoEmpresa, cancellationToken);
        return empresa is null ? NotFound() : Ok(empresa);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(EmpresaDbDto), StatusCodes.Status201Created)]
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
        var creada = await _empresasRepository.GetByIdAsync(dto.CodigoEmpresa, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { codigoEmpresa = dto.CodigoEmpresa.Trim() }, creada);
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
