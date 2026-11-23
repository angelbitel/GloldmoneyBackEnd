using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Application.Empenios.Vencidos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

/// <summary>Retiro de contratos vencidos por la casa y su reversión (SADE: Gerencia - Contratos Vencidos).</summary>
[ApiController]
[Route("api/contratos-vencidos")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ContratosVencidosController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContratosVencidosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Contratos vencidos (modo=vencidos) o retirados por la casa (modo=retirados).
    /// tipoFecha: todos | creacion | vencimiento. filtro: todos | mesesPagados | mesesDebe.
    /// operador: igual | menor | mayor | menorIgual | mayorIgual | diferente.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ContratosVencidosResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Buscar(
        [FromQuery] string codigoEmpresa,
        [FromQuery] int? codigoGrupo,
        [FromQuery] ModoVencidos modo = ModoVencidos.Vencidos,
        [FromQuery] TipoFechaFiltro tipoFecha = TipoFechaFiltro.Todos,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] FiltroMeses filtro = FiltroMeses.Todos,
        [FromQuery] OperadorMeses operador = OperadorMeses.Igual,
        [FromQuery] int meses = 0,
        [FromQuery] string? buscar = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = BusquedaVencidos.TamanoPaginaPorDefecto,
        CancellationToken cancellationToken = default)
    {
        var query = new BuscarContratosVencidosQuery(
            new FiltroContratosVencidos(codigoEmpresa, codigoGrupo, modo, tipoFecha, desde, hasta, filtro, operador, meses),
            buscar, pagina, tamanoPagina);
        return Ok(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>Claves de todos los contratos que cumplen los filtros y la búsqueda (para seleccionarlos todos).</summary>
    [HttpGet("claves")]
    [ProducesResponseType(typeof(IReadOnlyList<ContratoVencidoClaveDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Claves(
        [FromQuery] string codigoEmpresa,
        [FromQuery] int? codigoGrupo,
        [FromQuery] ModoVencidos modo = ModoVencidos.Vencidos,
        [FromQuery] TipoFechaFiltro tipoFecha = TipoFechaFiltro.Todos,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] FiltroMeses filtro = FiltroMeses.Todos,
        [FromQuery] OperadorMeses operador = OperadorMeses.Igual,
        [FromQuery] int meses = 0,
        [FromQuery] string? buscar = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ClavesContratosVencidosQuery(
            new FiltroContratosVencidos(codigoEmpresa, codigoGrupo, modo, tipoFecha, desde, hasta, filtro, operador, meses),
            buscar);
        return Ok(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>Prendas, pagos y comentarios del contrato.</summary>
    [HttpGet("{codigoEmpresa}/{codigoGrupo:int}/{numeroContrato}")]
    [ProducesResponseType(typeof(DetalleContratoVencidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle(
        string codigoEmpresa, int codigoGrupo, string numeroContrato,
        [FromQuery] ModoVencidos modo = ModoVencidos.Vencidos,
        CancellationToken cancellationToken = default)
    {
        var clave = new ContratoClave(codigoEmpresa, codigoGrupo, numeroContrato);
        return Ok(await _mediator.Send(new DetalleContratoVencidoQuery(clave, modo), cancellationToken));
    }

    [HttpPost("retirar")]
    [ProducesResponseType(typeof(ProcesoVencidosResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Retirar([FromBody] ProcesoContratosRequest request, CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual();
        if (usuario is null) return Unauthorized();

        return Ok(await _mediator.Send(new RetirarContratosVencidosCommand(request.Contratos ?? [], usuario), cancellationToken));
    }

    [HttpPost("deshacer")]
    [ProducesResponseType(typeof(ProcesoVencidosResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deshacer([FromBody] ProcesoContratosRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new DeshacerRetiroContratosCommand(request.Contratos ?? []), cancellationToken));
    }

    private string? UsuarioActual() =>
        User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name);
}

public sealed record ProcesoContratosRequest(IReadOnlyList<ContratoClave>? Contratos);
