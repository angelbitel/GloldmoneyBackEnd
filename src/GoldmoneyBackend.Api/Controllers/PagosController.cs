using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Pagos;
using GoldmoneyBackend.Application.Pagos.Commands.RegistrarAbonos;
using GoldmoneyBackend.Application.Pagos.Commands.RegistrarRetiros;
using GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;
using GoldmoneyBackend.Application.Pagos.Queries.GetContratosCobro;
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

    /// <summary>
    /// Devuelve los contratos activos de un cliente con lo que debe cada uno a la fecha de sesión
    /// (saldo de capital, saldo de interés, meses que debe e interés mensual).
    /// </summary>
    [HttpGet("clientes/{cedula}/contratos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ContratoCobroDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContratosCobro(string cedula, CancellationToken cancellationToken)
    {
        var contratos = await _mediator.Send(new GetContratosCobroQuery(cedula), cancellationToken);
        return Ok(contratos);
    }

    /// <summary>Devuelve las cuotas de interés pendientes de un contrato a la fecha de sesión y el total a pagar.</summary>
    [HttpGet("contratos/{id}/pendientes")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(PagosPendientesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPagosPendientes(string id, CancellationToken cancellationToken)
    {
        var pagos = await _mediator.Send(new GetPagosPendientesQuery(id), cancellationToken);
        return Ok(pagos);
    }

    /// <summary>
    /// Registra abonos a interés y/o capital sobre uno o varios contratos. Cada contrato se
    /// graba en su propia transacción: la respuesta indica, por contrato, el comprobante o el motivo del rechazo.
    /// </summary>
    [HttpPost("abonos")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(IReadOnlyList<AbonoRegistradoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarAbonos([FromBody] RegistrarAbonosRequest request, CancellationToken cancellationToken)
    {
        if (UsuarioResponsable() is not { } usuarioResponsable)
        {
            return Unauthorized();
        }

        var contratos = (request.Contratos ?? [])
            .Select(c => new AbonoContratoItem(c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato, c.Cuotas, c.AbonoCapital))
            .ToList();

        var resultados = await _mediator.Send(new RegistrarAbonosCommand(contratos, usuarioResponsable), cancellationToken);
        return Ok(resultados);
    }

    /// <summary>
    /// Retira uno o varios contratos cobrando su saldo de capital e interés a la fecha de sesión.
    /// Cada contrato se graba en su propia transacción: la respuesta indica, por contrato, el comprobante o el motivo del rechazo.
    /// </summary>
    [HttpPost("retiros")]
    [Authorize(Policy = AuthorizationPolicies.ClientesWrite)]
    [ProducesResponseType(typeof(IReadOnlyList<RetiroRegistradoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarRetiros([FromBody] RegistrarRetirosRequest request, CancellationToken cancellationToken)
    {
        if (UsuarioResponsable() is not { } usuarioResponsable)
        {
            return Unauthorized();
        }

        var contratos = (request.Contratos ?? [])
            .Select(c => new RetiroContratoItem(c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato))
            .ToList();

        var resultados = await _mediator.Send(new RegistrarRetirosCommand(contratos, usuarioResponsable), cancellationToken);
        return Ok(resultados);
    }

    /// <summary>Datos de la empresa y parámetros para calcular el retiro e imprimir recibos y etiquetas.</summary>
    [HttpGet("empresas/{codigoEmpresa}/configuracion")]
    [Authorize(Policy = AuthorizationPolicies.ClientesRead)]
    [ProducesResponseType(typeof(ConfiguracionCobroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguracionCobro(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var configuracion = await _mediator.Send(new GetConfiguracionCobroQuery(codigoEmpresa), cancellationToken);
        return Ok(configuracion);
    }

    private string? UsuarioResponsable()
    {
        var usuario = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name);

        return string.IsNullOrWhiteSpace(usuario) ? null : usuario;
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