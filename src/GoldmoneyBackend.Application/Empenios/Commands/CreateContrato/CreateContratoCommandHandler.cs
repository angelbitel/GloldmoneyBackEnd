using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

public sealed class CreateContratoCommandHandler : IRequestHandler<CreateContratoCommand, CreateContratoResultDto>
{
    private const string ProcesoPagosContratos = "PagosContratos";
    private const string ProcesoEmpeniosActivos = "EmpeniosActivos";
    private const string ProcesoEmpeniosNuevos = "EmpeniosNuevos";

    private readonly ILegacyEmpenioRepository _repository;
    private readonly ILogger<CreateContratoCommandHandler> _logger;

    public CreateContratoCommandHandler(
        ILegacyEmpenioRepository repository,
        ILogger<CreateContratoCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateContratoResultDto> Handle(CreateContratoCommand request, CancellationToken cancellationToken)
    {
        var dto = MapToDto(request);

        if (!EsProcesoValido(dto.ProcesoKey))
            throw new DomainValidationException("Estimado usuario, el tipo de proceso indicado no es reconocido por el sistema.");

        if (dto.FechaCreacion.Year < 1000)
            throw new DomainValidationException("Estimado usuario, es posible que la fecha ingresada no cumpla con el formato apropiado para el sistema.");

        if (dto.ConfirmadoPorUsuario == false)
            throw new DomainValidationException("Estimado usuario, debe confirmar la transaccion antes de salvar el contrato.");

        if (EsProcesoActivos(dto.ProcesoKey) && string.IsNullOrWhiteSpace(dto.NumeroContrato))
            throw new DomainValidationException("Estimado usuario, no ha ingresado un numero de contrato valido para el contrato a ingresar.");

        var empresa = await _repository.GetEmpresaByCodigoAsync(dto.CodigoEmpresa, cancellationToken);
        if (empresa is null)
            throw new NotFoundDomainException("Estimado usuario, el sistema no pudo encontrar la empresa solicitada.");

        if (dto.ControlaCajaPorUsuario == true && string.IsNullOrWhiteSpace(dto.UsuarioResponsable))
            throw new DomainValidationException("Estimado usuario, no se encontro el usuario responsable para el movimiento de caja.");

        if (dto.MontoMinimoEmpenio.HasValue && dto.CapitalPrestado < dto.MontoMinimoEmpenio.Value)
            throw new DomainValidationException("El capital del contrato es menor que la cantidad minima establecida en la empresa.");

        if (empresa.MontoAuxiliar.HasValue && empresa.MontoAuxiliar.Value < dto.CapitalPrestado)
            throw new DomainValidationException("Estimado usuario, el monto sobre el cual se dispone efectuar el empenio supera la cantidad en caja de la empresa.");

        var existe = await _repository.ContratoExistsAsync(dto.CodigoEmpresa.Trim(), dto.CodigoGrupo, dto.NumeroContrato.Trim(), cancellationToken);
        if (existe)
            throw new ConflictDomainException("Ya existe un contrato con esa clave.");

        if (dto.Detalles is null || dto.Detalles.Count == 0)
            throw new DomainValidationException("Estimado usuario, debe ingresar al menos un detalle para el nuevo contrato.");

        foreach (var detalle in dto.Detalles)
        {
            if (detalle.CodigoTipoPrenda <= 0)
                throw new DomainValidationException("Estimado usuario, cada detalle debe contener un codigo de tipo de prenda valido.");

            var existePrenda = await _repository.CategoriaPrendaExistsAsync(detalle.CodigoTipoPrenda, cancellationToken);
            if (!existePrenda)
                throw new DomainValidationException($"Estimado usuario, el tipo de prenda con codigo {detalle.CodigoTipoPrenda} no existe en el sistema.");
        }

        var codigoEmpresa = dto.CodigoEmpresa.Trim();
        var numeroContrato = dto.NumeroContrato.Trim();
        var codigoBarra = ConstruirCodigoBarra(codigoEmpresa, dto.CodigoGrupo, numeroContrato);

        try
        {
            if (empresa.MontoAuxiliar.HasValue)
                await _repository.UpdateMontoAuxiliarAsync(codigoEmpresa, empresa.MontoAuxiliar.Value - dto.CapitalPrestado, cancellationToken);

            var clienteExistente = await _repository.GetClienteByIdAsync(dto.IdCliente, cancellationToken);
            if (clienteExistente is null)
            {
                _repository.AddCliente(new ClienteDbUpsertDto(
                    dto.IdCliente, dto.Apellido, dto.Nombre, dto.Telefono,
                    1, dto.Direccion, null, dto.CodigoPais, null, null, null));
            }

            _repository.AddContrato(dto);

            foreach (var detalle in dto.Detalles)
            {
                var ultimaSecuencia = await _repository.GetUltimaSecuenciaDetalleAsync(codigoEmpresa, dto.CodigoGrupo, numeroContrato, cancellationToken);
                _repository.AddDetalle(detalle, codigoEmpresa, dto.CodigoGrupo, numeroContrato, ultimaSecuencia + 1, detalle.CodigoTipoPrenda, dto.ControlReloj);
            }

            var codigoTransaccion = EsProcesoActivos(dto.ProcesoKey) ? "EA" : "EN";
            var ultimoMovimiento = await _repository.GetUltimoNumeroMovimientoAsync(codigoEmpresa, cancellationToken);
            var ahora = DateTime.UtcNow;

            _repository.AddMovimientoCaja(dto, ultimoMovimiento + 1, codigoTransaccion, ahora);
            _repository.AddMovimientoTemporal(dto, ultimoMovimiento + 1, codigoTransaccion, ahora);

            await _repository.SaveChangesAsync(cancellationToken);

            return new CreateContratoResultDto(codigoBarra);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear contrato {CodigoEmpresa}/{CodigoGrupo}/{NumeroContrato}", codigoEmpresa, dto.CodigoGrupo, numeroContrato);
            throw;
        }
    }

    private static CrearEmpenioContratoDto MapToDto(CreateContratoCommand request)
    {
        var detalles = request.Detalles?.Select(d => new CrearEmpenioDetalleDto(
            d.CodigoTipoPrenda, d.Descripcion, d.Peso, d.Kilataje,
            d.Cantidad, d.MontoAvaluo, d.MontoPrestamoDetalle, d.Observacion)).ToList();

        return new CrearEmpenioContratoDto(
            request.CodigoEmpresa, request.CodigoGrupo, request.NumeroContrato,
            request.IdCliente, request.Serie, request.FechaCreacion,
            request.CapitalPrestado, request.Interes, request.SaldoActual,
            request.Mensualidad, request.Observacion, request.UltimaFechaPago,
            request.SaldoCapital, request.FechaVencimiento, request.PlazoPago,
            request.Nombre, request.Apellido, request.Direccion, request.Telefono,
            request.MontoMaximo, request.UsuarioResponsable, request.CodigoPais,
            request.ProcesoKey, request.MontoMinimoEmpenio, request.ControlaCajaPorUsuario,
            request.ConfirmadoPorUsuario, request.TrabajoConKilates, request.ControlReloj,
            request.TipoTransaccion, detalles);
    }

    private static bool EsProcesoValido(string procesoKey) =>
        string.Equals(procesoKey?.Trim(), ProcesoPagosContratos, StringComparison.OrdinalIgnoreCase)
        || string.Equals(procesoKey?.Trim(), ProcesoEmpeniosActivos, StringComparison.OrdinalIgnoreCase)
        || string.Equals(procesoKey?.Trim(), ProcesoEmpeniosNuevos, StringComparison.OrdinalIgnoreCase);

    private static bool EsProcesoActivos(string procesoKey) =>
        string.Equals(procesoKey?.Trim(), ProcesoEmpeniosActivos, StringComparison.OrdinalIgnoreCase);

    private static string ConstruirCodigoBarra(string codigoEmpresa, int codigoGrupo, string numeroContrato) =>
        string.Concat(codigoEmpresa.Trim(), codigoGrupo.ToString(), numeroContrato.Trim());
}
