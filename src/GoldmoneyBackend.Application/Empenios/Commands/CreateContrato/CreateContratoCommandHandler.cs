using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

public sealed class CreateContratoCommandHandler : IRequestHandler<CreateContratoCommand, CreateContratoResultDto>
{
    private readonly IEmpeniosDataService _empeniosDataService;

    public CreateContratoCommandHandler(IEmpeniosDataService empeniosDataService)
    {
        _empeniosDataService = empeniosDataService;
    }

    public async Task<CreateContratoResultDto> Handle(CreateContratoCommand request, CancellationToken cancellationToken)
    {
        var detalles = request.Detalles?
            .Select(d => new CrearEmpenioDetalleDto(
                d.CodigoTipoPrenda,
                d.Descripcion,
                d.Peso,
                d.Kilataje,
                d.Cantidad,
                d.MontoAvaluo,
                d.MontoPrestamoDetalle,
                d.Observacion))
            .ToList();

        var dto = new CrearEmpenioContratoDto(
            request.CodigoEmpresa,
            request.CodigoGrupo,
            request.NumeroContrato,
            request.IdCliente,
            request.Serie,
            request.FechaCreacion,
            request.CapitalPrestado,
            request.Interes,
            request.SaldoActual,
            request.Mensualidad,
            request.Observacion,
            request.UltimaFechaPago,
            request.SaldoCapital,
            request.FechaVencimiento,
            request.PlazoPago,
            request.Nombre,
            request.Apellido,
            request.Direccion,
            request.Telefono,
            request.MontoMaximo,
            request.UsuarioResponsable,
            request.CodigoPais,
            request.ProcesoKey,
            request.MontoMinimoEmpenio,
            request.ControlaCajaPorUsuario,
            request.ConfirmadoPorUsuario,
            request.TrabajoConKilates,
            request.ControlReloj,
            request.TipoTransaccion,
            detalles);

        var contratoId = await _empeniosDataService.CrearContratoAsync(dto, cancellationToken);

        return new CreateContratoResultDto(contratoId);
    }
}
