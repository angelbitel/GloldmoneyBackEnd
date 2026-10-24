using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class PagosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PagosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Devuelve la lista de pagos pendientes de un contrato con su desglose y el total a pagar.</summary>
    [HttpGet("contratos/{id}/pendientes")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(PagosPendientesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPagosPendientes(string id, CancellationToken cancellationToken)
    {
        var pagos = await _mediator.Send(new GetPagosPendientesQuery(id), cancellationToken);
        return Ok(pagos);
    }

    /// <summary>Devuelve únicamente el valor total a pagar de un contrato.</summary>
    [HttpGet("contratos/{id}/total")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTotalPagar(string id, CancellationToken cancellationToken)
    {
        var pagos = await _mediator.Send(new GetPagosPendientesQuery(id), cancellationToken);
        return Ok(new { totalPagar = pagos.TotalPagar, cantidadPagos = pagos.CantidadPagos });
    }
}