using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Empenios;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

public sealed class CreateContratoCommandHandler : IRequestHandler<CreateContratoCommand, CreateContratoResultDto>
{
    /// <summary>Código de transacción del VB6 para un empeño nuevo en MOVIMIENTO_CAJA.</summary>
    private const string TransaccionEmpenioNuevo = "EN";

    /// <summary>Números ya ocupados que se saltan si la secuencia quedó por detrás de CONTRATOS.</summary>
    private const int IntentosNumeracion = 100;

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
        var codigoEmpresa = request.CodigoEmpresa.Trim();
        var idCliente = request.IdCliente.Trim();

        var empresa = await _repository.GetEmpresaByCodigoAsync(codigoEmpresa, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el sistema no pudo encontrar la empresa solicitada.");

        var fechaSesion = await _repository.GetFechaSesionAbiertaAsync(codigoEmpresa, cancellationToken)
            ?? throw new DomainValidationException("Estimado usuario, no hay una sesión de caja abierta para la empresa.");

        var grupo = await _repository.GetGrupoAsync(codigoEmpresa, request.CodigoGrupo, cancellationToken);
        if (grupo is null || !grupo.Activo)
            throw new DomainValidationException("Estimado usuario, el grupo seleccionado no existe o no está activo.");
        if (string.IsNullOrWhiteSpace(grupo.Abreviatura))
            throw new DomainValidationException("Estimado usuario, el grupo seleccionado no tiene abreviatura para numerar sus contratos.");
        if (grupo.PlazoMeses <= 0)
            throw new DomainValidationException("Estimado usuario, el grupo seleccionado no tiene un plazo válido.");

        if (empresa.MontoAuxiliar.HasValue && empresa.MontoAuxiliar.Value < request.CapitalPrestado)
            throw new DomainValidationException("Estimado usuario, el monto sobre el cual se dispone efectuar el empenio supera la cantidad en caja de la empresa.");

        foreach (var codigoPrenda in request.Detalles.Select(d => d.CodigoTipoPrenda).Distinct())
        {
            if (!await _repository.CategoriaPrendaExistsAsync(codigoPrenda, cancellationToken))
                throw new DomainValidationException($"Estimado usuario, el tipo de prenda con codigo {codigoPrenda} no existe en el sistema.");
        }

        var clienteExistente = await _repository.GetClienteByIdAsync(idCliente, cancellationToken);
        if (clienteExistente is null && (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido)))
            throw new DomainValidationException("Estimado usuario, el cliente no existe: indique su nombre y apellido para registrarlo.");

        var calculo = CalculoEmpenio.Calcular(request.CapitalPrestado, grupo.TasaInteres, grupo.PlazoMeses, fechaSesion);

        try
        {
            // Una sola transacción: la secuencia, la caja y el contrato se revierten juntos si algo falla.
            return await _repository.EnTransaccionAsync(async ct =>
            {
                if (empresa.MontoAuxiliar.HasValue)
                {
                    try
                    {
                        await _repository.DescontarCajaAsync(codigoEmpresa, request.CapitalPrestado, ct);
                    }
                    catch (InvalidOperationException)
                    {
                        // Otro movimiento simultáneo dejó la caja por debajo del capital.
                        throw new DomainValidationException("Estimado usuario, el monto sobre el cual se dispone efectuar el empenio supera la cantidad en caja de la empresa.");
                    }
                }

                var (numeroContrato, serie) = await ReservarNumeroAsync(codigoEmpresa, request.CodigoGrupo, grupo.Abreviatura!.Trim(), ct);

                if (clienteExistente is null)
                {
                    _repository.AddCliente(new ClienteDbUpsertDto(
                        idCliente, request.Apellido?.Trim(), request.Nombre?.Trim(), request.Telefono?.Trim(),
                        1, request.Direccion?.Trim(), null, request.CodigoPais?.Trim(), null, null, null));
                }

                var contrato = new CrearEmpenioContratoDto(
                    codigoEmpresa,
                    request.CodigoGrupo,
                    numeroContrato,
                    serie,
                    idCliente,
                    calculo.FechaCreacion,
                    request.CapitalPrestado,
                    grupo.TasaInteres,
                    calculo.InteresMensual,
                    calculo.UltimaFechaPago,
                    calculo.FechaVencimiento,
                    grupo.PlazoMeses,
                    request.UsuarioResponsable.Trim());

                _repository.AddContrato(contrato);

                // El contrato es nuevo: sus detalles se numeran desde 1.
                var secuencia = 0;
                foreach (var detalle in request.Detalles)
                {
                    _repository.AddDetalle(
                        new CrearEmpenioDetalleDto(detalle.CodigoTipoPrenda, detalle.Descripcion, detalle.Peso, detalle.Kilataje, detalle.Cantidad),
                        codigoEmpresa, request.CodigoGrupo, numeroContrato, ++secuencia);
                }

                await _repository.AgregarMovimientoCajaAsync(contrato, TransaccionEmpenioNuevo, ct);
                await _repository.SaveChangesAsync(ct);

                return new CreateContratoResultDto(
                    string.Concat(codigoEmpresa, request.CodigoGrupo.ToString(), numeroContrato),
                    codigoEmpresa,
                    request.CodigoGrupo,
                    numeroContrato,
                    serie,
                    calculo.FechaCreacion,
                    calculo.FechaVencimiento,
                    request.CapitalPrestado,
                    grupo.TasaInteres,
                    calculo.InteresMensual,
                    grupo.PlazoMeses);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            _logger.LogError(ex, "Error al crear contrato {CodigoEmpresa}/{CodigoGrupo} para {IdCliente}", codigoEmpresa, request.CodigoGrupo, idCliente);
            throw;
        }
    }

    /// <summary>
    /// Toma la siguiente secuencia del grupo. Si ese número ya existe en CONTRATOS (la secuencia
    /// quedó por detrás), sigue con la próxima, igual que la vista previa del número.
    /// </summary>
    private async Task<(string NumeroContrato, string Serie)> ReservarNumeroAsync(
        string codigoEmpresa, int codigoGrupo, string abreviatura, CancellationToken cancellationToken)
    {
        for (var intento = 0; intento < IntentosNumeracion; intento++)
        {
            var secuencia = await _repository.SiguienteSecuenciaGrupoAsync(codigoEmpresa, abreviatura, cancellationToken);
            var numero = CalculoEmpenio.Numerar(codigoEmpresa, codigoGrupo, secuencia);

            if (!await _repository.ContratoExistsAsync(codigoEmpresa, codigoGrupo, numero.NumeroContrato, cancellationToken))
                return numero;
        }

        throw new DomainValidationException(
            $"Estimado usuario, no se encontró un número de contrato libre para el grupo {abreviatura}. Revise la secuencia en SECUENCIAS_EMPRESA.");
    }
}
