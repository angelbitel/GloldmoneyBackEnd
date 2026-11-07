using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Retira contratos igual que <c>Salvar_Retiro_de_Contrato</c> del VB6 (<c>sp_ad_retiro_contratos</c>,
/// <c>sp_actualizar_campos_contratos</c>, <c>sp_ad_Historial_Contrato</c>, <c>sp_Actualizar_Caja_Empresa</c>,
/// <c>sp_ad_Movimiento_Caja</c> y el paso del contrato al historial). Ver <see cref="OperacionesCajaLegacy"/>.
/// </summary>
public sealed class RetirosRepository : IRetirosRepository
{
    private const string TransaccionRetiroCapital = "MR";
    private const string TransaccionRetiroInteres = "IR";
    private const string StatusRetiro = "RC";
    private const string StatusHistorialPagos = "AI";

    private readonly LegacyDataDbContext _dbContext;

    public RetirosRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken) =>
        OperacionesCajaLegacy.EnTransaccionAsync(_dbContext, operacion, cancellationToken);

    public async Task<ContratoRetiroDto?> GetContratoBloqueadoAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        var contrato = await OperacionesCajaLegacy.GetContratoBloqueadoAsync(_dbContext, codigoEmpresa, codigoGrupo, numeroContrato, cancellationToken);

        if (contrato is null)
        {
            return null;
        }

        // El retiro trabaja con sentencias SQL; la entidad no debe quedar en el ChangeTracker.
        _dbContext.Entry(contrato).State = EntityState.Detached;

        return new ContratoRetiroDto(
            contrato.CodigoEmpresa.Trim(),
            contrato.CodigoGrupo,
            contrato.NumeroContrato.Trim(),
            contrato.IdCliente.Trim(),
            contrato.FechaCreacion,
            contrato.UltimaFechaPago,
            contrato.FechaVencimiento,
            contrato.SaldoCapital ?? 0m,
            contrato.Interes ?? 0m);
    }

    public async Task<SesionRetiroDto> GetSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var (fechaSesion, diasGracia) = await OperacionesCajaLegacy.GetSesionAbiertaAsync(_dbContext, codigoEmpresa, cancellationToken);

        var restarAbonoCapital = await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .MaxAsync(x => x.RestarAbonoCapital, cancellationToken);

        return new SesionRetiroDto(fechaSesion, diasGracia, restarAbonoCapital == 1m);
    }

    public async Task<int> RegistrarRetiroAsync(RegistroRetiroDto registro, CancellationToken cancellationToken)
    {
        var retiro = registro.Retiro;
        var empresa = registro.CodigoEmpresa;
        var grupo = registro.CodigoGrupo;
        var contrato = registro.NumeroContrato;
        var usuario = registro.UsuarioResponsable;
        var ahora = OperacionesCajaLegacy.AhoraLocal();

        // El VB6 graba la fecha del pago con la hora, el historial con la hora sobre el 1900-01-01,
        // y los pagos y copias pasados al historial con la hora de VB (Time) sobre el 1899-12-30.
        var fechaPago = registro.FechaSesion.Date.Add(new TimeSpan(ahora.Hour, ahora.Minute, ahora.Second));
        var horaIngreso = OperacionesCajaLegacy.SoloHora(ahora);
        var horaVb = new DateTime(1899, 12, 30).Add(new TimeSpan(ahora.Hour, ahora.Minute, ahora.Second));

        // 1. Secuencia del recibo.
        var numeroRecibo = await OperacionesCajaLegacy.SiguienteSecuenciaAsync(_dbContext, empresa, OperacionesCajaLegacy.SecuenciaRecibos, cancellationToken);

        // 2. Retiro (sp_ad_retiro_contratos). El VB6 envía la cédula del cliente como cédula y nombre
        //    del retirante, y "Copia" si al contrato se le emitió una copia de recibo.
        await EjecutarAsync(1, $"""
            INSERT INTO RETIROS (codigo_empresa, codigo_grupo, numero_contrato, id_cliente, fecha_pago, comentario,
                pago_capital, pago_interes, cedula_retirante, calidad_recibo, nombre_retirante, usuario_responsable, secuencia_recibo)
            VALUES ({empresa}, {grupo}, {contrato}, {registro.IdCliente}, {fechaPago}, NULL,
                {retiro.PagoCapital}, {retiro.PagoInteres}, {registro.IdCliente},
                CASE WHEN EXISTS (SELECT 1 FROM COPIA_RECIBO WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato})
                     THEN 'Copia' ELSE 'Original' END,
                {registro.IdCliente}, {usuario}, {numeroRecibo})
            """, "registrar el retiro", cancellationToken);

        // 3. Contrato en cero, con las fechas corridas por los meses que debía (sp_actualizar_campos_contratos).
        await EjecutarAsync(1, $"""
            UPDATE CONTRATOS SET saldo_capital = 0, saldo_actual = 0,
                ultima_fecha_pago = {retiro.NuevaUltimaFechaPago}, fecha_vencimiento = {retiro.NuevoVencimiento}
            WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato}
            """, "actualizar el contrato", cancellationToken);

        // 4. Historial del contrato (sp_ad_Historial_Contrato, status RC). El VB6 pasa la hora de creación
        //    del contrato como "hh:mm:ss", que queda sobre el 1900-01-01.
        await EjecutarAsync(1, $"""
            INSERT INTO HISTORIAL_CONTRATOS (codigo_empresa, codigo_grupo, numero_contrato, status_historial, id_cliente,
                fecha_creacion, fecha_vencimiento, capital_prestado, tasa_interes, interes_mensual, plazo_pago,
                ultima_fecha_pago, saldo, saldo_capital, usuario_responsable, fecha_ingreso, hora_ingreso,
                usuario_creacion, hora_creacion, monto_maximo)
            SELECT codigo_empresa, codigo_grupo, numero_contrato, {StatusRetiro}, id_cliente,
                fecha_creacion, fecha_vencimiento, capital_prestado, interes, interes_mensual, plazo_pago,
                ultima_fecha_pago, saldo_actual, saldo_capital, {usuario}, {registro.FechaSesion.Date}, {horaIngreso},
                usuario_responsable,
                DATEADD(second, DATEDIFF(second, CAST(CAST(hora_transaccion AS date) AS datetime), hora_transaccion), CAST('19000101' AS datetime)),
                monto_maximo
            FROM CONTRATOS
            WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato}
            """, "pasar el contrato al historial", cancellationToken);

        // 5. Caja de la empresa (sp_Actualizar_Caja_Empresa, incremento).
        await OperacionesCajaLegacy.ActualizarCajaEmpresaAsync(_dbContext, empresa, retiro.Total, cancellationToken);

        // 6. Movimientos: el capital siempre y el interés si no es cero (negativo si estaba adelantado).
        await OperacionesCajaLegacy.AgregarMovimientoAsync(
            _dbContext, empresa, grupo, contrato, TransaccionRetiroCapital, retiro.PagoCapital,
            registro.FechaSesion, ahora, usuario, cancellationToken);

        if (retiro.PagoInteres != 0)
        {
            await OperacionesCajaLegacy.AgregarMovimientoAsync(
                _dbContext, empresa, grupo, contrato, TransaccionRetiroInteres, retiro.PagoInteres,
                registro.FechaSesion, ahora, usuario, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 7. Pagos al historial.
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO HISTORIAL_PAGOS (codigo_empresa, numero_pagos, codigo_grupo, numero_contrato, status_h_pagos,
                fecha_pago, cantidad_pagos, pago_capital, pago_interes, saldo_capital, saldo_interes,
                ultima_fecha_pago, usuario_responsable, secuencia_recibo, hora_transaccion)
            SELECT codigo_empresa, numero_pagos, codigo_grupo, numero_contrato, {StatusHistorialPagos},
                fecha_pago, cantidad_pagos, pago_capital, pago_interes, saldo_capital, saldo_interes,
                ultima_fecha_pago, usuario_responsable, secuencia_recibo, {horaVb}
            FROM PAGOS
            WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato};

            DELETE FROM PAGOS WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato};
            """, cancellationToken);

        // 8. Copias de recibo al historial (conservan su hora, sobre el 1899-12-30).
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO H_COPIAS_RECIBO (codigo_empresa, numero_copia, codigo_grupo, numero_contrato, id_cliente,
                fecha_copia, hora_copia, capital, usuario_responsable, secuencia_recibo)
            SELECT codigo_empresa, numero_copia, codigo_grupo, numero_contrato, id_cliente,
                fecha, DATEADD(second, DATEDIFF(second, CAST(CAST(hora AS date) AS datetime), hora), CAST('18991230' AS datetime)),
                capital, usuario_responsable, secuencia_recibo
            FROM COPIA_RECIBO
            WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato};

            DELETE FROM COPIA_RECIBO WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato};
            """, cancellationToken);

        // 9. Prendas al historial y borrado del contrato original.
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO H_DETALLES_CONTRATOS (codigo_empresa, codigo_grupo, numero_contrato, numero_detalle, descripcion,
                kilates, peso, codigo_reloj, cantidad_producto, codigo_categoria_prenda)
            SELECT codigo_empresa, codigo_grupo, numero_contrato, secuencia_contratos, descripcion,
                kilates, peso, codigo_reloj, cantidad_producto, codigo_categoria_prenda
            FROM DETALLES_CONTRATOS
            WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato};

            DELETE FROM DETALLES_CONTRATOS WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato};
            """, cancellationToken);

        await EjecutarAsync(1, $"""
            DELETE FROM CONTRATOS WHERE codigo_empresa = {empresa} AND codigo_grupo = {grupo} AND numero_contrato = {contrato}
            """, "eliminar el contrato original", cancellationToken);

        return numeroRecibo;
    }

    private async Task EjecutarAsync(int filasEsperadas, FormattableString sql, string paso, CancellationToken cancellationToken)
    {
        var filas = await _dbContext.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);

        if (filas != filasEsperadas)
        {
            throw new InvalidOperationException($"No se pudo {paso}: {filas} filas afectadas.");
        }
    }
}
