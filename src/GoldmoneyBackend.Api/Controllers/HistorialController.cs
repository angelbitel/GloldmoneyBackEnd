using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Historial;
using GoldmoneyBackend.Application.Historial.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

/// <summary>Historial de pagos, contratos cerrados de un cliente y métricas de cartera.</summary>
[ApiController]
[Route("api/historial")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class HistorialController : ControllerBase
{
    private readonly IMediator _mediator;

    public HistorialController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Pagos de un contrato (activos e históricos), del más reciente al más antiguo.</summary>
    [HttpGet("contratos/{codigoEmpresa}/{codigoGrupo:int}/{numeroContrato}/pagos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PagoHistorialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagosContrato(
        string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        var pagos = await _mediator.Send(
            new GetPagosContratoQuery(codigoEmpresa, codigoGrupo, numeroContrato), cancellationToken);
        return Ok(pagos);
    }

    /// <summary>Contratos del cliente que ya salieron de cartera: retirados (RC), rematados (CF) y anulados (AC).</summary>
    [HttpGet("clientes/{cedula}")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(HistorialClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistorialCliente(string cedula, CancellationToken cancellationToken)
    {
        var historial = await _mediator.Send(new GetHistorialClienteQuery(cedula), cancellationToken);
        return Ok(historial);
    }

    /// <summary>Métricas de cartera de la empresa; con <c>cedula</c>, solo de ese cliente.</summary>
    [HttpGet("metricas/{codigoEmpresa}")]
    [ProducesResponseType(typeof(MetricasCarteraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetricas(
        string codigoEmpresa, [FromQuery] string? cedula, CancellationToken cancellationToken)
    {
        var metricas = await _mediator.Send(new GetMetricasCarteraQuery(codigoEmpresa, cedula), cancellationToken);
        return Ok(metricas);
    }
}
