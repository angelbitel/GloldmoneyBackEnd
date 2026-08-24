using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class EmpeniosDataService : IEmpeniosDataService
{
    private const string ProcesoPagosContratos = "PagosContratos";
    private const string ProcesoEmpeniosActivos = "EmpeniosActivos";
    private const string ProcesoEmpeniosNuevos = "EmpeniosNuevos";

    private readonly LegacyDataDbContext _dbContext;

    public EmpeniosDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
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

        if (dto.Serie.Trim().Length < 10)
        {
            throw new DomainValidationException("Estimado usuario, verifique el numero de Contrato, ya que no cumple con el formato establecido para efectuar el empenio.");
        }

        if (dto.ConfirmadoPorUsuario == false)
        {
            throw new DomainValidationException("Estimado usuario, debe confirmar la transaccion antes de salvar el contrato.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (empresa.MontoAuxiliar.HasValue && empresa.MontoAuxiliar.Value < dto.CapitalPrestado)
        {
            throw new DomainValidationException("Estimado usuario, el monto sobre el cual se dispone efectuar el empenio supera la cantidad en caja de la empresa.");
        }

        if (empresa.MontoAuxiliar.HasValue)
        {
            empresa.MontoAuxiliar -= dto.CapitalPrestado;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        try
        {
            var valorRetorno = await EjecutarSpCreacionContratoAsync(dto, transaction, cancellationToken);

            if (!valorRetorno.HasValue)
            {
                throw new DomainValidationException("Estimado usuario, el proceso para grabar los datos no devolvio valores apropiados para la aplicacion.");
            }

            if (valorRetorno.Value != 0)
            {
                throw new DomainValidationException("Estimado usuario, un error dentro de los procesos utilizados para grabar el contrato fallo, verifique el mismo y vuelva a intentarlo.");
            }

            var codigoBarra = ConstruirCodigoBarra(dto.CodigoEmpresa, dto.CodigoGrupo, dto.NumeroContrato);

            await GuardarDetalleContratoAsync(dto, codigoBarra, transaction, cancellationToken);
            await IngresarMovimientoCajaAsync(dto, codigoBarra, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return codigoBarra;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DomainValidationException($"No se pudo crear el contrato mediante sp_creacion_contratos. Detalle: {ex.Message}");
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

    private async Task<int?> EjecutarSpCreacionContratoAsync(
        CrearEmpenioContratoDto dto,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = @"
            EXEC sp_creacion_contratos
                @codigo_empresa,
                @codigo_grupo,
                @numero_contrato,
                @id_cliente,
                @serie,
                @fecha_creacion,
                @capital_prestado,
                @interes,
                @saldo_actual,
                @mensualidad,
                @observacion,
                @ultima_fecha_pago,
                @saldo_capital,
                @fecha_vencimiento,
                @plazo_pago,
                @nombre,
                @apellido,
                @direccion,
                @telefono,
                @monto_maximo,
                @usuario_responsable,
                @codigo_pais";

        AddParameter(command, "@codigo_empresa", dto.CodigoEmpresa.Trim());
        AddParameter(command, "@codigo_grupo", dto.CodigoGrupo);
        AddParameter(command, "@numero_contrato", dto.NumeroContrato.Trim());
        AddParameter(command, "@id_cliente", TrimOrDbNull(dto.IdCliente));
        AddParameter(command, "@serie", dto.Serie.Trim());
        AddParameter(command, "@fecha_creacion", dto.FechaCreacion);
        AddParameter(command, "@capital_prestado", dto.CapitalPrestado);
        AddParameter(command, "@interes", dto.Interes);
        AddParameter(command, "@saldo_actual", dto.SaldoActual);
        AddParameter(command, "@mensualidad", dto.Mensualidad);
        AddParameter(command, "@observacion", TrimOrDbNull(dto.Observacion));
        AddParameter(command, "@ultima_fecha_pago", dto.UltimaFechaPago);
        AddParameter(command, "@saldo_capital", dto.SaldoCapital);
        AddParameter(command, "@fecha_vencimiento", dto.FechaVencimiento);
        AddParameter(command, "@plazo_pago", dto.PlazoPago);
        AddParameter(command, "@nombre", ToProperCase(dto.Nombre));
        AddParameter(command, "@apellido", ToProperCase(dto.Apellido));
        AddParameter(command, "@direccion", TrimOrDbNull(dto.Direccion));
        AddParameter(command, "@telefono", TrimOrDbNull(dto.Telefono));
        AddParameter(command, "@monto_maximo", dto.MontoMaximo);
        AddParameter(command, "@usuario_responsable", dto.UsuarioResponsable.Trim());
        AddParameter(command, "@codigo_pais", dto.CodigoPais.Trim());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(result);
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
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var detalle in dto.Detalles!)
        {
            if (string.IsNullOrWhiteSpace(detalle.CodigoTipoPrenda))
            {
                throw new DomainValidationException("Estimado usuario, cada detalle debe contener un codigo de tipo de prenda.");
            }

            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = @"
                EXEC sp_salvar_detalle_contrato
                    @codigo_barra,
                    @codigo_grupo,
                    @trabajo_kilates,
                    @control_reloj,
                    @codigo_tipo_prenda,
                    @descripcion,
                    @peso,
                    @kilataje,
                    @cantidad,
                    @monto_avaluo,
                    @monto_prestamo_detalle,
                    @observacion";

            AddParameter(command, "@codigo_barra", codigoBarra);
            AddParameter(command, "@codigo_grupo", dto.CodigoGrupo);
            AddParameter(command, "@trabajo_kilates", dto.TrabajoConKilates ?? 0);
            AddParameter(command, "@control_reloj", dto.ControlReloj ?? 0);
            AddParameter(command, "@codigo_tipo_prenda", detalle.CodigoTipoPrenda.Trim());
            AddParameter(command, "@descripcion", TrimOrDbNull(detalle.Descripcion));
            AddParameter(command, "@peso", detalle.Peso ?? 0m);
            AddParameter(command, "@kilataje", detalle.Kilataje ?? 0m);
            AddParameter(command, "@cantidad", detalle.Cantidad ?? 1);
            AddParameter(command, "@monto_avaluo", detalle.MontoAvaluo ?? 0m);
            AddParameter(command, "@monto_prestamo_detalle", detalle.MontoPrestamoDetalle ?? 0m);
            AddParameter(command, "@observacion", TrimOrDbNull(detalle.Observacion));

            await command.ExecuteNonQueryAsync(cancellationToken);
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
