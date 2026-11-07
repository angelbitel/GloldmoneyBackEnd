using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Pagos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarRetiros;

public sealed class RegistrarRetirosCommandHandler : IRequestHandler<RegistrarRetirosCommand, IReadOnlyList<RetiroRegistradoDto>>
{
    private const string ErrorInesperado = "Estimado usuario, ocurrió un error al momento de retirar el contrato.";

    private readonly IRetirosRepository _repository;
    private readonly ILogger<RegistrarRetirosCommandHandler> _logger;

    public RegistrarRetirosCommandHandler(IRetirosRepository repository, ILogger<RegistrarRetirosCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetiroRegistradoDto>> Handle(RegistrarRetirosCommand request, CancellationToken cancellationToken)
    {
        var usuario = request.UsuarioResponsable.Trim();
        var resultados = new List<RetiroRegistradoDto>(request.Contratos.Count);

        // Como el VB6, cada contrato se confirma por separado: un fallo solo revierte ese contrato.
        foreach (var item in request.Contratos)
        {
            var codigoEmpresa = item.CodigoEmpresa.Trim();
            var numeroContrato = item.NumeroContrato.Trim();

            try
            {
                var comprobante = await _repository.EnTransaccionAsync(
                    ct => RetirarAsync(codigoEmpresa, item.CodigoGrupo, numeroContrato, usuario, ct),
                    cancellationToken);

                resultados.Add(new RetiroRegistradoDto(codigoEmpresa, item.CodigoGrupo, numeroContrato, true, null, comprobante));
            }
            catch (DomainException ex)
            {
                resultados.Add(new RetiroRegistradoDto(codigoEmpresa, item.CodigoGrupo, numeroContrato, false, ex.Message, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al retirar el contrato {CodigoEmpresa}/{CodigoGrupo}/{NumeroContrato}", codigoEmpresa, item.CodigoGrupo, numeroContrato);
                resultados.Add(new RetiroRegistradoDto(codigoEmpresa, item.CodigoGrupo, numeroContrato, false, ErrorInesperado, null));
            }
        }

        return resultados;
    }

    private async Task<ComprobanteRetiroDto> RetirarAsync(
        string codigoEmpresa,
        int codigoGrupo,
        string numeroContrato,
        string usuario,
        CancellationToken cancellationToken)
    {
        var contrato = await _repository.GetContratoBloqueadoAsync(codigoEmpresa, codigoGrupo, numeroContrato, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el contrato no existe o ya fue retirado.");

        var sesion = await _repository.GetSesionAbiertaAsync(codigoEmpresa, cancellationToken);
        if (sesion.FechaSesion is not { } fechaSesion)
        {
            throw new DomainValidationException("Estimado usuario, la empresa no tiene una sesión abierta; inicie la sesión antes de registrar pagos.");
        }

        if (contrato.FechaCreacion is not { } fechaCreacion || contrato.FechaVencimiento is not { } fechaVencimiento)
        {
            throw new DomainValidationException("Estimado usuario, las fechas de vencimiento y pagado hasta no son válidas dentro del sistema.");
        }

        // Al crear el contrato la última fecha de pago es la de creación.
        var ultimaFechaPago = contrato.UltimaFechaPago ?? fechaCreacion;

        var retiro = CalculoRetiro.Calcular(
            new ContratoParaRetiro(fechaCreacion, ultimaFechaPago, fechaVencimiento, contrato.SaldoCapital, contrato.Interes),
            fechaSesion,
            sesion.DiasGracia,
            sesion.RestarAbonoCapital);

        var numeroRecibo = await _repository.RegistrarRetiroAsync(
            new RegistroRetiroDto(codigoEmpresa, codigoGrupo, numeroContrato, contrato.IdCliente, fechaSesion, usuario, retiro),
            cancellationToken);

        return new ComprobanteRetiroDto(
            numeroRecibo,
            fechaSesion,
            contrato.IdCliente,
            retiro.MesesQueDebe,
            retiro.PagoCapital,
            retiro.PagoInteres,
            retiro.Total,
            retiro.NuevaUltimaFechaPago,
            retiro.NuevoVencimiento);
    }
}
