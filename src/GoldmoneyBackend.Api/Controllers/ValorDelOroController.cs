using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.ValorDelOro;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/valor-del-oro")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ValorDelOroController : ControllerBase
{
    private readonly IValorDelOroRepository _valorDelOroRepository;

    public ValorDelOroController(IValorDelOroRepository valorDelOroRepository)
    {
        _valorDelOroRepository = valorDelOroRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ValorDelOroDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _valorDelOroRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoEmpresa}/{statusCalidad}/{kilataje:decimal}")]
    [ProducesResponseType(typeof(ValorDelOroDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        CancellationToken cancellationToken)
    {
        var valor = await _valorDelOroRepository.GetByKeyAsync(
            codigoEmpresa,
            statusCalidad,
            kilataje,
            cancellationToken);
        return valor is null ? NotFound() : Ok(valor);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ValorDelOroDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateValorDelOroRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new ValorDelOroDbUpsertDto(
            request.CodigoEmpresa,
            request.StatusCalidad,
            request.Kilataje,
            request.MaximoValor,
            request.MinimoValor);

        await _valorDelOroRepository.CreateAsync(dto, cancellationToken);
        var created = await _valorDelOroRepository.GetByKeyAsync(
            request.CodigoEmpresa,
            request.StatusCalidad,
            request.Kilataje,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByKey),
            new
            {
                codigoEmpresa = request.CodigoEmpresa,
                statusCalidad = request.StatusCalidad,
                kilataje = request.Kilataje
            },
            created);
    }

    [HttpPut("{codigoEmpresa}/{statusCalidad}/{kilataje:decimal}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ValorDelOroDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        [FromBody] UpdateValorDelOroRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new ValorDelOroDbUpsertDto(
            codigoEmpresa,
            statusCalidad,
            kilataje,
            request.MaximoValor,
            request.MinimoValor);

        await _valorDelOroRepository.UpdateAsync(
            codigoEmpresa,
            statusCalidad,
            kilataje,
            dto,
            cancellationToken);
        var updated = await _valorDelOroRepository.GetByKeyAsync(
            codigoEmpresa,
            statusCalidad,
            kilataje,
            cancellationToken);

        return Ok(updated);
    }

    [HttpDelete("{codigoEmpresa}/{statusCalidad}/{kilataje:decimal}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        CancellationToken cancellationToken)
    {
        await _valorDelOroRepository.DeleteAsync(
            codigoEmpresa,
            statusCalidad,
            kilataje,
            cancellationToken);
        return NoContent();
    }
}
