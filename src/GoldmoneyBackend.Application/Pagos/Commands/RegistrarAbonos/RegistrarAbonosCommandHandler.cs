using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Pagos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarAbonos;

public sealed class RegistrarAbonosCommandHandler : IRequestHandler<RegistrarAbonosCommand, IReadOnlyList<AbonoRegistradoDto>>
{
    private const string ErrorInesperado = "Estimado usuario, ocurrió un error al momento de abonar el contrato.";

    private readonly IAbonosRepository _repository;
    private readonly ILogger<RegistrarAbonosCommandHandler> _logger;

    public RegistrarAbonosCommandHandler(IAbonosRepository repository, ILogger<RegistrarAbonosCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AbonoRegistradoDto>> Handle(RegistrarAbonosCommand request, CancellationToken cancellationToken)
    {
        var usuario = request.UsuarioResponsable.Trim();
        var resultados = new List<AbonoRegistradoDto>(request.Contratos.Count);

        // Como el VB6, cada contrato se confirma por separado: un fallo solo revierte ese contrato.
        foreach (var item in request.Contratos)
        {
            var codigoEmpresa = item.CodigoEmpresa.Trim();
            var numeroContrato = item.NumeroContrato.Trim();

            try
            {
                var comprobante = await _repository.EnTransaccionAsync(
                    ct => RegistrarAsync(codigoEmpresa, item.CodigoGrupo, numeroContrato, item, usuario, ct),
                    cancellationToken);

                resultados.Add(new AbonoRegistradoDto(codigoEmpresa, item.CodigoGrupo, numeroContrato, true, null, comprobante));
            }
            catch (DomainException ex)
            {
                resultados.Add(new AbonoRegistradoDto(codigoEmpresa, item.CodigoGrupo, numeroContrato, false, ex.Message, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al abonar el contrato {CodigoEmpresa}/{CodigoGrupo}/{NumeroContrato}", codigoEmpresa, item.CodigoGrupo, numeroContrato);
                resultados.Add(new AbonoRegistradoDto(codigoEmpresa, item.CodigoGrupo, numeroContrato, false, ErrorInesperado, null));
            }
        }

        return resultados;
    }

    private async Task<ComprobanteAbonoDto> RegistrarAsync(
        string codigoEmpresa,
        int codigoGrupo,
        string numeroContrato,
        AbonoContratoItem item,
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

        var abono = CalculoAbono.Calcular(
            new ContratoParaAbono(fechaCreacion, ultimaFechaPago, fechaVencimiento, contrato.SaldoCapital, contrato.Interes),
            fechaSesion,
            sesion.DiasGracia,
            item.Cuotas,
            item.AbonoCapital);

        var numeros = await _repository.RegistrarAbonoAsync(
            new RegistroAbonoDto(codigoEmpresa, codigoGrupo, numeroContrato, fechaSesion, usuario, abono),
            cancellationToken);

        return new ComprobanteAbonoDto(
            numeros.NumeroPago,
            numeros.NumeroRecibo,
            fechaSesion,
            abono.Cuotas,
            abono.AbonoInteres,
            abono.AbonoCapital,
            abono.Total,
            abono.NuevoSaldoCapital,
            abono.NuevoSaldoInteres,
            abono.NuevoInteresMensual,
            abono.NuevaUltimaFechaPago,
            abono.NuevoVencimiento);
    }
}
