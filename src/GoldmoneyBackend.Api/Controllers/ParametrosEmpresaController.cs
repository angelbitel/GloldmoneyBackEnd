using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.ParametrosEmpresa;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/parametros-empresa")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class ParametrosEmpresaController : ControllerBase
{
    private readonly IParametrosEmpresaRepository _parametrosEmpresaRepository;

    public ParametrosEmpresaController(IParametrosEmpresaRepository parametrosEmpresaRepository)
    {
        _parametrosEmpresaRepository = parametrosEmpresaRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ParametrosEmpresaDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _parametrosEmpresaRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoEmpresa}")]
    [ProducesResponseType(typeof(ParametrosEmpresaDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var parametros = await _parametrosEmpresaRepository.GetByKeyAsync(codigoEmpresa, cancellationToken);
        return parametros is null ? NotFound() : Ok(parametros);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ParametrosEmpresaDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateParametrosEmpresaRequest request, CancellationToken cancellationToken)
    {
        var dto = new ParametrosEmpresaDbUpsertDto(
            request.CodigoEmpresa,
            request.PermitirDescuento,
            request.TrabajarKilates,
            request.ExistenciaReloj,
            request.BloquearPlazoInt,
            request.CodigoBarra,
            request.OperarAbonoParcial,
            request.AbonarVencidos,
            request.AbonarCapital,
            request.TipoCobroInteres,
            request.DiasGracia,
            request.ImprimirEmpActivos,
            request.ImprimirCopia,
            request.OperarEtiquetas,
            request.ControlarPuerto,
            request.PuertoContratos,
            request.PuertoPagos,
            request.TipoImpRecibo,
            request.ControlarCapital,
            request.TipoControlCapital,
            request.TipoValorCapital,
            request.ContMontoCaja,
            request.CostoCopia,
            request.AnulacionControladaTiempo,
            request.ImpEtiquetaCopia,
            request.DetallePrestablecido,
            request.RestarAbonoCapital,
            request.CapitalSinDecimal,
            request.PermitirPagoAdelantado,
            request.ImprimirReciboPago,
            request.ModeloImpresoraCodBar,
            request.ControlarAnulacion,
            request.ControlarImpresionContratos,
            request.ControlarProcesoAnulacion,
            request.NoEtiquetasPagos,
            request.MontoMinContrato,
            request.TipoEtiquetaPago,
            request.ImprimirEtiquetaRetiro,
            request.TiempoAnulacion,
            request.RepModuloSoporte,
            request.TipoMontoMaximo,
            request.PorcentajeMontoMaximo,
            request.ManejoCierreAutomatico,
            request.HoraCierreAutomatico,
            request.UltimoCierreAutomatico,
            request.StatusManejoScaner,
            request.RutaDirImagenes,
            request.MaxLengthCharDescripcion,
            request.MostrarComentarioCliente,
            request.MostrarComentarioContrato,
            request.MostrarColumnaCantProducto,
            request.NoEtiquetasRetiros);

        await _parametrosEmpresaRepository.CreateAsync(dto, cancellationToken);
        var created = await _parametrosEmpresaRepository.GetByKeyAsync(request.CodigoEmpresa, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { codigoEmpresa = request.CodigoEmpresa }, created);
    }

    [HttpPut("{codigoEmpresa}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ParametrosEmpresaDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        string codigoEmpresa,
        [FromBody] UpdateParametrosEmpresaRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new ParametrosEmpresaDbUpsertDto(
            codigoEmpresa,
            request.PermitirDescuento,
            request.TrabajarKilates,
            request.ExistenciaReloj,
            request.BloquearPlazoInt,
            request.CodigoBarra,
            request.OperarAbonoParcial,
            request.AbonarVencidos,
            request.AbonarCapital,
            request.TipoCobroInteres,
            request.DiasGracia,
            request.ImprimirEmpActivos,
            request.ImprimirCopia,
            request.OperarEtiquetas,
            request.ControlarPuerto,
            request.PuertoContratos,
            request.PuertoPagos,
            request.TipoImpRecibo,
            request.ControlarCapital,
            request.TipoControlCapital,
            request.TipoValorCapital,
            request.ContMontoCaja,
            request.CostoCopia,
            request.AnulacionControladaTiempo,
            request.ImpEtiquetaCopia,
            request.DetallePrestablecido,
            request.RestarAbonoCapital,
            request.CapitalSinDecimal,
            request.PermitirPagoAdelantado,
            request.ImprimirReciboPago,
            request.ModeloImpresoraCodBar,
            request.ControlarAnulacion,
            request.ControlarImpresionContratos,
            request.ControlarProcesoAnulacion,
            request.NoEtiquetasPagos,
            request.MontoMinContrato,
            request.TipoEtiquetaPago,
            request.ImprimirEtiquetaRetiro,
            request.TiempoAnulacion,
            request.RepModuloSoporte,
            request.TipoMontoMaximo,
            request.PorcentajeMontoMaximo,
            request.ManejoCierreAutomatico,
            request.HoraCierreAutomatico,
            request.UltimoCierreAutomatico,
            request.StatusManejoScaner,
            request.RutaDirImagenes,
            request.MaxLengthCharDescripcion,
            request.MostrarComentarioCliente,
            request.MostrarComentarioContrato,
            request.MostrarColumnaCantProducto,
            request.NoEtiquetasRetiros);

        await _parametrosEmpresaRepository.UpdateAsync(codigoEmpresa, dto, cancellationToken);
        var updated = await _parametrosEmpresaRepository.GetByKeyAsync(codigoEmpresa, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{codigoEmpresa}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string codigoEmpresa, CancellationToken cancellationToken)
    {
        await _parametrosEmpresaRepository.DeleteAsync(codigoEmpresa, cancellationToken);
        return NoContent();
    }
}
