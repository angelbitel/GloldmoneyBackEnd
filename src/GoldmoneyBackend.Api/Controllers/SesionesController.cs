using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Sesiones;
using GoldmoneyBackend.Application.Caja;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;
using GoldmoneyBackend.Application.Sesiones.Commands.ActualizarSaldos;
using GoldmoneyBackend.Application.Sesiones.Commands.CerrarSesion;
using GoldmoneyBackend.Application.Sesiones.Commands.ContinuarSesion;
using GoldmoneyBackend.Application.Sesiones.Queries.GetEstadoSesiones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/sesiones")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class SesionesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAdministracionReadService _administracion;

    public SesionesController(IMediator mediator, IAdministracionReadService administracion)
    {
        _mediator = mediator;
        _administracion = administracion;
    }

    /// <summary>Estado de la caja: sesión abierta, monto actual y movimientos del día de la sesión.</summary>
    [HttpGet("estado-caja/{codigoEmpresa}")]
    [ProducesResponseType(typeof(EstadoCajaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEstadoCaja(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var estado = await _administracion.GetEstadoCajaAsync(codigoEmpresa, cancellationToken);
        return estado is null ? NotFound() : Ok(estado);
    }

    /// <summary>Últimos 10 depósitos (DD) o retiros (RD) de dinero de la caja.</summary>
    [HttpGet("caja/{codigoEmpresa}/movimientos")]
    [ProducesResponseType(typeof(IReadOnlyList<MovimientoCajaRecienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovimientosManuales(string codigoEmpresa, [FromQuery] string tipo, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new MovimientosCajaRecientesQuery(codigoEmpresa, tipo), cancellationToken));
    }

    /// <summary>Depósito o retiro de dinero en caja (SADE: Modificación de caja). Requiere la clave del usuario o de un administrador.</summary>
    [HttpPost("caja/{codigoEmpresa}/movimientos")]
    [ProducesResponseType(typeof(MovimientoCajaManualResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegistrarMovimientoManual(
        string codigoEmpresa, [FromBody] MovimientoCajaRequest request, CancellationToken cancellationToken)
    {
        var usuarioConectado = User.UsuarioResponsable();
        if (string.IsNullOrWhiteSpace(usuarioConectado)) return Unauthorized();

        var command = new RegistrarMovimientoCajaCommand(
            codigoEmpresa, request.Tipo, request.Monto, request.Motivo, request.Usuario, request.Clave, usuarioConectado);
        var resultado = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Vista previa de "Actualizar saldos": contratos y saldos que cambiarían, sin modificar datos.</summary>
    [HttpGet("caja/{codigoEmpresa}/saldos")]
    [ProducesResponseType(typeof(ActualizacionSaldosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PrevisualizarSaldos(string codigoEmpresa, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ActualizarSaldosCommand(codigoEmpresa, false), cancellationToken));

    /// <summary>Actualizar saldos (SADE sp_Actualizar_Saldos_de_la_Empresa) de los contratos que usan esta caja.</summary>
    [HttpPost("caja/{codigoEmpresa}/actualizar-saldos")]
    [ProducesResponseType(typeof(ActualizacionSaldosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarSaldos(string codigoEmpresa, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ActualizarSaldosCommand(codigoEmpresa, true), cancellationToken));

    [HttpGet("empresas-pendientes")]
    [ProducesResponseType(typeof(IReadOnlyList<EstadoSesionEmpresaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmpresasPendientes(CancellationToken cancellationToken)
    {
        var estados = await _mediator.Send(new GetEstadoSesionesQuery(), cancellationToken);
        return Ok(estados);
    }

    /// <summary>
    /// Abre la caja de la empresa para hoy (fecha local), como el VB6, con el PIN del usuario.
    /// Responde 400 si el PIN falta o no es correcto, 409 si la empresa ya tiene una caja abierta
    /// o ya tuvo sesión hoy y 429 tras varios intentos seguidos.
    /// </summary>
    [HttpPost("abrir")]
    [EnableRateLimiting(RateLimitPolicies.VerificacionPin)]
    [ProducesResponseType(typeof(SesionAbiertaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Abrir([FromBody] AbrirSesionRequest request, CancellationToken cancellationToken)
    {
        if (User.UsuarioResponsable() is not { } usuarioResponsable)
        {
            return Unauthorized();
        }

        var command = new AbrirSesionCommand(request.CodigoEmpresa, usuarioResponsable, request.Pin ?? string.Empty);
        var sesion = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, sesion);
    }

    /// <summary>Cierre de caja: registra el valor final y deja la sesión cerrada (status 1).</summary>
    [HttpPost("cerrar")]
    [ProducesResponseType(typeof(SesionCerradaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cerrar([FromBody] CerrarSesionRequest request, CancellationToken cancellationToken)
    {
        var usuarioConectado = User.UsuarioResponsable();
        if (string.IsNullOrWhiteSpace(usuarioConectado)) return Unauthorized();

        var command = new CerrarSesionCommand(request.CodigoEmpresa, request.Usuario, request.Clave, usuarioConectado);
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>Entrar a la sesión de caja ya abierta: valida la contraseña (no modifica datos).</summary>
    [HttpPost("continuar")]
    [ProducesResponseType(typeof(GoldmoneyBackend.Application.Empenios.Transacciones.SesionOperacionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Continuar([FromBody] CerrarSesionRequest request, CancellationToken cancellationToken)
    {
        var usuarioConectado = User.UsuarioResponsable();
        if (string.IsNullOrWhiteSpace(usuarioConectado)) return Unauthorized();

        var command = new ContinuarSesionCommand(request.CodigoEmpresa, request.Usuario, request.Clave, usuarioConectado);
        return Ok(await _mediator.Send(command, cancellationToken));
    }
}

public sealed record MovimientoCajaRequest(string Tipo, decimal Monto, string Motivo, string Usuario, string Clave);

public sealed record CerrarSesionRequest(string CodigoEmpresa, string Usuario, string Clave);
