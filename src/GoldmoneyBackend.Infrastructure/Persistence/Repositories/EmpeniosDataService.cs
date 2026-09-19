using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class EmpeniosDataService : IEmpeniosDataService
{
    private const string ProcesoPagosContratos = "PagosContratos";
    private const string ProcesoEmpeniosActivos = "EmpeniosActivos";
    private const string ProcesoEmpeniosNuevos = "EmpeniosNuevos";

    private readonly LegacyDataDbContext _dbContext;
    private readonly ILogger<EmpeniosDataService> _logger;

    public EmpeniosDataService(LegacyDataDbContext dbContext, ILogger<EmpeniosDataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> CrearContratoAsync(CrearEmpenioContratoDto dto, CancellationToken cancellationToken)
    {
        ValidarDatosBase(dto);

        var empresa = await _dbContext.Empresas
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == dto.CodigoEmpresa.Trim(), cancellationToken);

        if (empresa is null)
        {
            throw new NotFoundDomainException("Estimado usuario, el sistema no pudo encontrar la empresa solicitada.");
        }

        if (dto.ControlaCajaPorUsuario == true && string.IsNullOrWhiteSpace(dto.UsuarioResponsable))
        {
            throw new DomainValidationException("Estimado usuario, no se encontro el usuario responsable para el movimiento de caja.");
        }

        if (dto.MontoMinimoEmpenio.HasValue && dto.CapitalPrestado < dto.MontoMinimoEmpenio.Value)
        {
            throw new DomainValidationException("El capital del contrato es menor que la cantidad minima establecida en la empresa.");
        }

        if (EsProcesoActivos(dto.ProcesoKey))
        {
            if (string.IsNullOrWhiteSpace(dto.NumeroContrato))
            {
                throw new DomainValidationException("Estimado usuario, no ha ingresado un numero de contrato valido para el contrato a ingresar.");
            }
        }

        if (dto.FechaCreacion.Year < 1000)
        {
            throw new DomainValidationException("Estimado usuario, es posible que la fecha ingresada no cumpla con el formato apropiado para el sistema.");
        }

        if (dto.Serie.Trim().Length < 10 || dto.Serie.Trim().Length > 20)
        {
            throw new DomainValidationException("Estimado usuario, verifique el numero de Contrato, ya que no cumple con el formato establecido para efectuar el empenio.");
        }

        if (dto.ConfirmadoPorUsuario == false)
        {
            throw new DomainValidationException("Estimado usuario, debe confirmar la transaccion antes de salvar el contrato.");
        }

        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                if (empresa.MontoAuxiliar.HasValue && empresa.MontoAuxiliar.Value < dto.CapitalPrestado)
                {
                    throw new DomainValidationException("Estimado usuario, el monto sobre el cual se dispone efectuar el empenio supera la cantidad en caja de la empresa.");
                }

                if (empresa.MontoAuxiliar.HasValue)
                {
                    empresa.MontoAuxiliar -= dto.CapitalPrestado;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                var valorRetorno = await CrearContratoYClienteAsync(dto, cancellationToken);

                if (!valorRetorno.HasValue)
                {
                    throw new DomainValidationException("Estimado usuario, el proceso para grabar los datos no devolvio valores apropiados para la aplicacion.");
                }

                if (valorRetorno.Value != 0)
                {
                    throw new DomainValidationException("Estimado usuario, un error dentro de los procesos utilizados para grabar el contrato fallo, verifique el mismo y vuelva a intentarlo.");
                }

                var codigoBarra = ConstruirCodigoBarra(dto.CodigoEmpresa, dto.CodigoGrupo, dto.NumeroContrato);

                await GuardarDetalleContratoAsync(dto, codigoBarra, cancellationToken);
                await IngresarMovimientoCajaAsync(dto, codigoBarra, transaction, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return codigoBarra;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear contrato: {CodigoEmpresa}/{CodigoGrupo}/{NumeroContrato}",
                    dto.CodigoEmpresa, dto.CodigoGrupo, dto.NumeroContrato);
                await transaction.RollbackAsync(cancellationToken);
                throw new DomainValidationException($"No se pudo crear el contrato en tabla CONTRATOS. Detalle: {ex.Message}");
            }
        }

    }

    private static void ValidarDatosBase(CrearEmpenioContratoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProcesoKey))
        {
            throw new DomainValidationException("Estimado usuario, no se ha indicado un tipo de proceso valido.");
        }

        if (!EsProcesoPagosContratos(dto.ProcesoKey) && !EsProcesoActivos(dto.ProcesoKey) && !EsProcesoNuevos(dto.ProcesoKey))
        {
            throw new DomainValidationException("Estimado usuario, el tipo de proceso indicado no es reconocido por el sistema.");
        }

        if (string.IsNullOrWhiteSpace(dto.CodigoEmpresa))
        {
            throw new DomainValidationException("Estimado usuario, no ha seleccionado una empresa sobre la cual grabar los datos en pantalla.");
        }

        if (!EsProcesoPagosContratos(dto.ProcesoKey) && string.IsNullOrWhiteSpace(dto.IdCliente))
        {
            throw new DomainValidationException("Estimado usuario, la identificacion del cliente o ID no es valido dentro del sistema.");
        }

        if (dto.FechaCreacion == default)
        {
            throw new DomainValidationException("Estimado usuario, la fecha con que se dispone grabar la transaccion no es valida dentro del sistema.");
        }

        if (dto.FechaCreacion.Year < 1000)
        {
            throw new DomainValidationException("Estimado usuario, el ano introducido no puede contener menos de cuatro digitos.");
        }

        if (dto.CodigoGrupo <= 0)
        {
            throw new DomainValidationException("Estimado usuario, no ha seleccionado un grupo clasificatorio para el nuevo contrato a ingresar.");
        }

        if (dto.CapitalPrestado <= 0)
        {
            throw new DomainValidationException("Estimado usuario, no ha ingresado el monto para el nuevo contrato.");
        }

        if (dto.SaldoCapital <= 0)
        {
            throw new DomainValidationException("Estimado usuario, el saldo a capital del nuevo contrato debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(dto.NumeroContrato))
        {
            throw new DomainValidationException("Estimado usuario, no ha ingresado un numero de contrato valido para el contrato a ingresar.");
        }

        if (string.IsNullOrWhiteSpace(dto.Serie))
        {
            throw new DomainValidationException("Estimado usuario, no ha ingresado una serie valida para el contrato a ingresar.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
        {
            throw new DomainValidationException("No existe un nombre o apellido en pantalla para salvar en presente contrato.");
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioResponsable))
        {
            throw new DomainValidationException("Estimado usuario, no se ha indicado un usuario responsable para la transaccion.");
        }

        if (string.IsNullOrWhiteSpace(dto.CodigoPais))
        {
            throw new DomainValidationException("Estimado usuario, no se ha indicado un codigo de pais valido para el cliente.");
        }

        if (dto.Detalles is null || dto.Detalles.Count == 0)
        {
            throw new DomainValidationException("Estimado usuario, debe ingresar al menos un detalle para el nuevo contrato.");
        }
    }

    private async Task<int?> CrearContratoYClienteAsync(
        CrearEmpenioContratoDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var codigoEmpresa = dto.CodigoEmpresa.Trim();
            var numeroContrato = dto.NumeroContrato.Trim();

            var existe = await _dbContext.Contratos
                .AsNoTracking()
                .AnyAsync(x => x.CodigoEmpresa == codigoEmpresa
                    && x.CodigoGrupo == dto.CodigoGrupo
                    && x.NumeroContrato == numeroContrato, cancellationToken);

            if (existe)
            {
                return 1;
            }

            var cliente = await _dbContext.Clientes
                .FirstOrDefaultAsync(x => x.IdCliente == dto.IdCliente, cancellationToken);

            if (cliente is null)
            {
                _dbContext.Clientes.Add(new ClienteDb
                {
                    IdCliente = dto.IdCliente,
                    Apellido = dto.Apellido,
                    Nombre = dto.Nombre,
                    Telefono = dto.Telefono,
                    Estatus = 1,
                    Direccion = dto.Direccion,
                    Comentario = null,
                    CodigoPais = dto.CodigoPais
                });
            }
            else
            {
                cliente.Apellido = dto.Apellido;
                cliente.Nombre = dto.Nombre;
                cliente.Telefono = dto.Telefono;
                cliente.Direccion = dto.Direccion;
                cliente.CodigoPais = dto.CodigoPais;
            }

            _dbContext.Contratos.Add(new ContratoDb
            {
                CodigoEmpresa = codigoEmpresa,
                CodigoGrupo = dto.CodigoGrupo,
                NumeroContrato = numeroContrato,
                IdCliente = dto.IdCliente,
                Serie = dto.Serie.Trim(),
                FechaCreacion = dto.FechaCreacion,
                CapitalPrestado = dto.CapitalPrestado,
                Interes = dto.Interes,
                SaldoActual = dto.SaldoActual,
                InteresMensual = dto.Mensualidad,
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
                UltimaFechaPago = dto.UltimaFechaPago,
                SaldoCapital = dto.SaldoCapital,
                FechaVencimiento = dto.FechaVencimiento,
                PlazoPago = dto.PlazoPago,
                UsuarioResponsable = dto.UsuarioResponsable.Trim(),
                HoraTransaccion = DateTime.UtcNow,
                MontoMaximo = dto.MontoMaximo
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return 0;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DbUpdateException al crear contrato {CodigoEmpresa}/{CodigoGrupo}/{NumeroContrato}",
                dto.CodigoEmpresa, dto.CodigoGrupo, dto.NumeroContrato);
            return 1;
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static object TrimOrDbNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }

    private async Task GuardarDetalleContratoAsync(
        CrearEmpenioContratoDto dto,
        string codigoBarra,
        CancellationToken cancellationToken)
    {
        foreach (var detalle in dto.Detalles!)
        {
            if (detalle.CodigoTipoPrenda <= 0)
            {
                throw new DomainValidationException("Estimado usuario, cada detalle debe contener un codigo de tipo de prenda valido.");
            }

            var codigoCategoriaPrenda = await _dbContext.CategoriasPrenda
                .AsNoTracking()
                .Select(x => (int?)x.CodigoCategoriaPrenda)
                .FirstOrDefaultAsync(x => x == detalle.CodigoTipoPrenda, cancellationToken);

            if (!codigoCategoriaPrenda.HasValue)
            {
                throw new DomainValidationException($"Estimado usuario, el tipo de prenda con codigo {detalle.CodigoTipoPrenda} no existe en el sistema.");
            }

            var ultimaSecuencia = await _dbContext.DetallesContratos
                .AsNoTracking()
                .Where(x => x.CodigoEmpresa == dto.CodigoEmpresa.Trim()
                    && x.CodigoGrupo == dto.CodigoGrupo
                    && x.NumeroContrato == dto.NumeroContrato.Trim())
                .MaxAsync(x => (int?)x.SecuenciaContratos) ?? 0;

            _dbContext.DetallesContratos.Add(new DetalleContratoDb
            {
                CodigoEmpresa = dto.CodigoEmpresa.Trim(),
                CodigoGrupo = dto.CodigoGrupo,
                NumeroContrato = dto.NumeroContrato.Trim(),
                SecuenciaContratos = ultimaSecuencia + 1,
                Descripcion = string.IsNullOrWhiteSpace(detalle.Descripcion) ? null : detalle.Descripcion.Trim(),
                Kilates = detalle.Kilataje,
                Peso = detalle.Peso,
                CodigoReloj = dto.ControlReloj,
                CantidadProducto = detalle.Cantidad ?? 1,
                CodigoCategoriaPrenda = codigoCategoriaPrenda.Value
            });

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException al guardar detalle del contrato {CodigoEmpresa}/{CodigoGrupo}/{NumeroContrato}",
                    dto.CodigoEmpresa, dto.CodigoGrupo, dto.NumeroContrato);
                throw new DomainValidationException($"No se pudo guardar el detalle del contrato en tabla DETALLES_CONTRATOS. Detalle: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }

    private async Task IngresarMovimientoCajaAsync(
        CrearEmpenioContratoDto dto,
        string codigoBarra,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var tipoTransaccion = string.IsNullOrWhiteSpace(dto.TipoTransaccion)
            ? (EsProcesoActivos(dto.ProcesoKey) ? "EA" : "EN")
            : dto.TipoTransaccion.Trim();

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = @"
            EXEC sp_ad_Movimiento_Caja
                @codigo_empresa,
                @codigo_grupo,
                @codigo_barra,
                @monto,
                @tipo_transaccion,
                @fecha_movimiento";

        AddParameter(command, "@codigo_empresa", dto.CodigoEmpresa.Trim());
        AddParameter(command, "@codigo_grupo", dto.CodigoGrupo);
        AddParameter(command, "@codigo_barra", codigoBarra);
        AddParameter(command, "@monto", dto.CapitalPrestado);
        AddParameter(command, "@tipo_transaccion", tipoTransaccion);
        AddParameter(command, "@fecha_movimiento", dto.FechaCreacion.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ConstruirCodigoBarra(string codigoEmpresa, int codigoGrupo, string numeroContrato)
    {
        return string.Concat(codigoEmpresa.Trim(), codigoGrupo.ToString(), numeroContrato.Trim());
    }

    private static bool EsProcesoActivos(string procesoKey)
    {
        return string.Equals(procesoKey?.Trim(), ProcesoEmpeniosActivos, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsProcesoPagosContratos(string procesoKey)
    {
        return string.Equals(procesoKey?.Trim(), ProcesoPagosContratos, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsProcesoNuevos(string procesoKey)
    {
        return string.Equals(procesoKey?.Trim(), ProcesoEmpeniosNuevos, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToProperCase(string value)
    {
        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }
}
