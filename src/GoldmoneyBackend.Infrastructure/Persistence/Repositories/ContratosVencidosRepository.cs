using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Application.Empenios.Vencidos;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reescritura de sp_Contratos_Vencidos_de_la_Empresa, sp_Contratos_Retirados_por_la_Empresa,
/// sp_ad_Historial_Contrato (status CF) y sp_ad_Restablecer_Contrato_Retirado.
/// Los traslados entre tablas se hacen con INSERT ... SELECT (como los SP) porque las columnas
/// legacy admiten NULL donde las entidades no (p. ej. codigo_categoria_prenda).
/// </summary>
public sealed class ContratosVencidosRepository : IContratosVencidosRepository
{
    private const string StatusRetiroCasa = "CF";
    private const string StatusPagoHistorial = "AI";

    private readonly LegacyDataDbContext _db;

    public ContratosVencidosRepository(LegacyDataDbContext db)
    {
        _db = db;
    }

    public async Task<ContextoVencimiento> GetContextoAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var empresa = await _db.Empresas
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .Select(x => new { x.CodigoEmpresa, x.ManejoCajaDep, x.CodEmpresaCaja })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, la empresa seleccionada no existe.");

        var codigoCaja = empresa.ManejoCajaDep == 1 && empresa.CodEmpresaCaja.HasValue
            ? empresa.CodEmpresaCaja.Value.ToString()
            : empresa.CodigoEmpresa.Trim();

        // Igual que el SP: la fecha de trabajo es la última apertura de la caja, esté abierta o no.
        var fechaSistema = await _db.SesionesEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoCaja)
            .MaxAsync(x => (DateTime?)x.FechaApertura, cancellationToken);

        var diasGracia = await _db.ParametrosEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .MaxAsync(x => x.DiasGracia, cancellationToken);

        return new ContextoVencimiento(fechaSistema, (int)(diasGracia ?? 0m));
    }

    public async Task<IReadOnlyList<ContratoVencidoDatos>> GetContratosAsync(FiltroContratosVencidos filtro, CancellationToken cancellationToken)
    {
        var desde = filtro.Desde?.Date;
        var hasta = filtro.Hasta?.Date.AddDays(1);
        var grupo = filtro.CodigoGrupo is > 0 ? filtro.CodigoGrupo : null;
        var porCreacion = filtro.TipoFecha == TipoFechaFiltro.Creacion && desde.HasValue;
        var porVencimiento = filtro.TipoFecha == TipoFechaFiltro.Vencimiento && desde.HasValue;

        // Los filtros se aplican sobre la entidad: EF no traduce un Where posterior a la proyección al record.
        if (filtro.Modo == ModoVencidos.Retirados)
        {
            var retirados = Retirados(filtro.CodigoEmpresa);
            if (grupo.HasValue) retirados = retirados.Where(x => x.CodigoGrupo == grupo.Value);
            if (porCreacion) retirados = retirados.Where(x => x.FechaCreacion >= desde && x.FechaCreacion < hasta);
            if (porVencimiento) retirados = retirados.Where(x => x.FechaVencimiento >= desde && x.FechaVencimiento < hasta);
            return await Proyectar(retirados).ToListAsync(cancellationToken);
        }

        var activos = Activos(filtro.CodigoEmpresa);
        if (grupo.HasValue) activos = activos.Where(x => x.CodigoGrupo == grupo.Value);
        if (porCreacion) activos = activos.Where(x => x.FechaCreacion >= desde && x.FechaCreacion < hasta);
        if (porVencimiento) activos = activos.Where(x => x.FechaVencimiento >= desde && x.FechaVencimiento < hasta);
        return await Proyectar(activos).ToListAsync(cancellationToken);
    }

    public Task<ContratoVencidoDatos?> GetContratoAsync(ContratoClave clave, ModoVencidos modo, CancellationToken cancellationToken) =>
        modo == ModoVencidos.Retirados
            ? Proyectar(Retirados(clave.CodigoEmpresa)
                .Where(x => x.CodigoGrupo == clave.CodigoGrupo && x.NumeroContrato == clave.NumeroContrato))
                .FirstOrDefaultAsync(cancellationToken)
            : Proyectar(Activos(clave.CodigoEmpresa)
                .Where(x => x.CodigoGrupo == clave.CodigoGrupo && x.NumeroContrato == clave.NumeroContrato))
                .FirstOrDefaultAsync(cancellationToken);

    public async Task<DetalleVencidoDatos> GetDetalleAsync(ContratoClave clave, ModoVencidos modo, CancellationToken cancellationToken)
    {
        var (emp, grp, num) = (clave.CodigoEmpresa, clave.CodigoGrupo, clave.NumeroContrato);
        List<PrendaVencidaDto> prendas;
        List<PagoContratoDto> pagos;
        string? idCliente;

        if (modo == ModoVencidos.Retirados)
        {
            prendas = await _db.HDetallesContratos.AsNoTracking()
                .Where(x => x.CodigoEmpresa == emp && x.CodigoGrupo == grp && x.NumeroContrato == num)
                .OrderBy(x => x.NumeroDetalle)
                .Select(x => new PrendaVencidaDto(x.NumeroDetalle, x.Descripcion, x.Kilates, x.Peso, x.CantidadProducto))
                .ToListAsync(cancellationToken);

            pagos = await _db.Database.SqlQuery<PagoContratoDto>($"""
                SELECT numero_pagos AS NumeroPago, fecha_pago AS FechaPago, CAST(cantidad_pagos AS decimal(18,2)) AS CantidadPagos,
                       CAST(pago_interes AS decimal(18,2)) AS PagoInteres, CAST(pago_capital AS decimal(18,2)) AS PagoCapital,
                       CAST(saldo_interes AS decimal(18,2)) AS SaldoInteres, CAST(saldo_capital AS decimal(18,2)) AS SaldoCapital,
                       ultima_fecha_pago AS PagadoHasta
                FROM HISTORIAL_PAGOS
                WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                """).ToListAsync(cancellationToken);

            idCliente = await _db.HistorialContratos.AsNoTracking()
                .Where(x => x.CodigoEmpresa == emp && x.CodigoGrupo == grp && x.NumeroContrato == num)
                .Select(x => x.IdCliente)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            prendas = await _db.DetallesContratos.AsNoTracking()
                .Where(x => x.CodigoEmpresa == emp && x.CodigoGrupo == grp && x.NumeroContrato == num)
                .OrderBy(x => x.SecuenciaContratos)
                .Select(x => new PrendaVencidaDto(x.SecuenciaContratos, x.Descripcion, x.Kilates, x.Peso, x.CantidadProducto))
                .ToListAsync(cancellationToken);

            pagos = await _db.Database.SqlQuery<PagoContratoDto>($"""
                SELECT numero_pagos AS NumeroPago, fecha_pago AS FechaPago, CAST(cantidad_pagos AS decimal(18,2)) AS CantidadPagos,
                       CAST(pago_interes AS decimal(18,2)) AS PagoInteres, CAST(pago_capital AS decimal(18,2)) AS PagoCapital,
                       CAST(saldo_interes AS decimal(18,2)) AS SaldoInteres, CAST(saldo_capital AS decimal(18,2)) AS SaldoCapital,
                       ultima_fecha_pago AS PagadoHasta
                FROM PAGOS
                WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                """).ToListAsync(cancellationToken);

            idCliente = await _db.Contratos.AsNoTracking()
                .Where(x => x.CodigoEmpresa == emp && x.CodigoGrupo == grp && x.NumeroContrato == num)
                .Select(x => x.IdCliente)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var comentario = idCliente is null
            ? null
            : await _db.Clientes.AsNoTracking()
                .Where(x => x.IdCliente == idCliente)
                .Select(x => x.Comentario)
                .FirstOrDefaultAsync(cancellationToken);

        var parametros = await _db.ParametrosEmpresa.AsNoTracking()
            .Where(x => x.CodigoEmpresa == emp)
            .Select(x => new { x.MostrarComentarioCliente, x.MostrarComentarioContrato })
            .FirstOrDefaultAsync(cancellationToken);

        return new DetalleVencidoDatos(
            prendas.Select(p => p with { Descripcion = p.Descripcion?.Trim() }).ToList(),
            pagos.OrderBy(x => x.NumeroPago).ToList(),
            comentario,
            parametros?.MostrarComentarioCliente == 1,
            parametros?.MostrarComentarioContrato == 1);
    }

    public async Task RetirarAsync(RetiroContratosDatos datos, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var c in datos.Contratos)
        {
            var (emp, grp, num) = (c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato);

            var enHistorial = await _db.HistorialContratos.AsNoTracking()
                .Where(x => x.CodigoEmpresa == emp && x.CodigoGrupo == grp && x.NumeroContrato == num)
                .Select(x => x.StatusHistorial)
                .FirstOrDefaultAsync(cancellationToken);
            if (enHistorial is not null)
                throw new ConflictDomainException($"Estimado usuario, el contrato {num} ya se encuentra en el historial (estado {enHistorial.Trim()}).");

            await EjecutarAsync(num, async () =>
            {
                // sp_ad_Historial_Contrato con status CF.
                var filas = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO HISTORIAL_CONTRATOS (codigo_empresa, codigo_grupo, numero_contrato, status_historial, id_cliente,
                        fecha_creacion, fecha_vencimiento, capital_prestado, tasa_interes, interes_mensual, plazo_pago,
                        ultima_fecha_pago, saldo, saldo_capital, usuario_responsable, fecha_ingreso, hora_ingreso,
                        usuario_creacion, hora_creacion, monto_maximo)
                    SELECT codigo_empresa, codigo_grupo, numero_contrato, {StatusRetiroCasa}, id_cliente,
                        fecha_creacion, fecha_vencimiento, capital_prestado, interes, interes_mensual, plazo_pago,
                        ultima_fecha_pago, saldo_actual, saldo_capital, {datos.UsuarioResponsable}, {datos.FechaIngreso}, {datos.HoraIngreso},
                        usuario_responsable, hora_transaccion, monto_maximo
                    FROM CONTRATOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                    """, cancellationToken);
                if (filas != 1)
                    throw new ConflictDomainException($"Estimado usuario, el contrato {num} ya no se encuentra activo.");

                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO H_DETALLES_CONTRATOS (codigo_empresa, codigo_grupo, numero_contrato, numero_detalle, descripcion,
                        kilates, peso, codigo_reloj, cantidad_producto, codigo_categoria_prenda)
                    SELECT codigo_empresa, codigo_grupo, numero_contrato, secuencia_contratos, descripcion,
                        kilates, peso, codigo_reloj, cantidad_producto, codigo_categoria_prenda
                    FROM DETALLES_CONTRATOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                    """, cancellationToken);

                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO HISTORIAL_PAGOS (codigo_empresa, numero_pagos, codigo_grupo, numero_contrato, status_h_pagos,
                        fecha_pago, cantidad_pagos, pago_capital, pago_interes, saldo_capital, saldo_interes,
                        ultima_fecha_pago, usuario_responsable, secuencia_recibo, hora_transaccion)
                    SELECT codigo_empresa, numero_pagos, codigo_grupo, numero_contrato, {StatusPagoHistorial},
                        fecha_pago, cantidad_pagos, pago_capital, pago_interes, saldo_capital, saldo_interes,
                        ultima_fecha_pago, usuario_responsable, secuencia_recibo, hora_transaccion
                    FROM PAGOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                    """, cancellationToken);

                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO H_COPIAS_RECIBO (codigo_empresa, numero_copia, codigo_grupo, numero_contrato, id_cliente,
                        fecha_copia, hora_copia, capital, usuario_responsable, secuencia_recibo)
                    SELECT codigo_empresa, numero_copia, codigo_grupo, numero_contrato, id_cliente,
                        fecha, hora, capital, usuario_responsable, secuencia_recibo
                    FROM COPIA_RECIBO
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                    """, cancellationToken);

                // Las FK en cascada eliminan DETALLES_CONTRATOS, PAGOS, COPIA_RECIBO, DESCUENTOS_INTERES y PRORROGA_VENCIMIENTO.
                var eliminados = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM CONTRATOS WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                    """, cancellationToken);
                if (eliminados != 1)
                    throw new ConflictDomainException($"Estimado usuario, no fue posible retirar el contrato {num}.");
            });
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task DeshacerRetiroAsync(IReadOnlyList<ContratoClave> contratos, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var c in contratos)
        {
            var (emp, grp, num) = (c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato);

            var activo = await _db.Contratos.AsNoTracking()
                .AnyAsync(x => x.CodigoEmpresa == emp && x.CodigoGrupo == grp && x.NumeroContrato == num, cancellationToken);
            if (activo)
                throw new ConflictDomainException($"Estimado usuario, el contrato {num} ya se encuentra activo.");

            await EjecutarAsync(num, async () =>
            {
                // sp_ad_Restablecer_Contrato_Retirado: el contrato vuelve con el usuario y la hora de creación originales.
                var filas = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO CONTRATOS (codigo_empresa, codigo_grupo, numero_contrato, id_cliente, serie, fecha_creacion,
                        capital_prestado, interes, saldo_actual, interes_mensual, observacion, ultima_fecha_pago,
                        saldo_capital, fecha_vencimiento, plazo_pago, usuario_responsable, hora_transaccion, monto_maximo)
                    SELECT codigo_empresa, codigo_grupo, numero_contrato, id_cliente, RIGHT(RTRIM(numero_contrato), 10), fecha_creacion,
                        capital_prestado, tasa_interes, saldo, interes_mensual, NULL, ultima_fecha_pago,
                        saldo_capital, fecha_vencimiento, plazo_pago, usuario_creacion, hora_creacion, monto_maximo
                    FROM HISTORIAL_CONTRATOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                      AND status_historial = {StatusRetiroCasa}
                    """, cancellationToken);
                if (filas != 1)
                    throw new NotFoundDomainException($"Estimado usuario, el contrato {num} no se encuentra entre los contratos retirados por la casa.");

                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO DETALLES_CONTRATOS (codigo_empresa, codigo_grupo, numero_contrato, secuencia_contratos, descripcion,
                        kilates, peso, codigo_reloj, cantidad_producto, codigo_categoria_prenda)
                    SELECT codigo_empresa, codigo_grupo, numero_contrato, numero_detalle, descripcion,
                        kilates, peso, codigo_reloj, cantidad_producto, codigo_categoria_prenda
                    FROM H_DETALLES_CONTRATOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                    """, cancellationToken);

                // Mejora sobre el SP original: también regresan los pagos y las copias (como sp_Retornar_Contrato_Retirado_al_Inventario).
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO PAGOS (codigo_empresa, numero_pagos, codigo_grupo, numero_contrato, fecha_pago, cantidad_pagos,
                        pago_interes, pago_capital, saldo_capital, saldo_interes, ultima_fecha_pago, usuario_responsable,
                        secuencia_recibo, hora_transaccion)
                    SELECT codigo_empresa, numero_pagos, codigo_grupo, numero_contrato, fecha_pago, cantidad_pagos,
                        pago_interes, pago_capital, saldo_capital, saldo_interes, ultima_fecha_pago, usuario_responsable,
                        secuencia_recibo, hora_transaccion
                    FROM HISTORIAL_PAGOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num};

                    DELETE FROM HISTORIAL_PAGOS WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num};

                    INSERT INTO COPIA_RECIBO (codigo_empresa, numero_copia, codigo_grupo, numero_contrato, id_cliente,
                        fecha, hora, capital, usuario_responsable, secuencia_recibo)
                    SELECT codigo_empresa, numero_copia, codigo_grupo, numero_contrato, id_cliente,
                        fecha_copia, hora_copia, capital, usuario_responsable, secuencia_recibo
                    FROM H_COPIAS_RECIBO
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num};

                    DELETE FROM H_COPIAS_RECIBO WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num};
                    """, cancellationToken);

                // La FK en cascada elimina H_DETALLES_CONTRATOS.
                var eliminados = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM HISTORIAL_CONTRATOS
                    WHERE codigo_empresa = {emp} AND codigo_grupo = {grp} AND numero_contrato = {num}
                      AND status_historial = {StatusRetiroCasa}
                    """, cancellationToken);
                if (eliminados != 1)
                    throw new ConflictDomainException($"Estimado usuario, no fue posible restablecer el contrato {num}.");
            });
        }

        await tx.CommitAsync(cancellationToken);
    }

    /// <summary>Convierte errores de SQL (p. ej. llaves duplicadas en las tablas H_*) en un mensaje legible.</summary>
    private static async Task EjecutarAsync(string numeroContrato, Func<Task> accion)
    {
        try
        {
            await accion();
        }
        catch (SqlException ex)
        {
            throw new ConflictDomainException(
                $"Estimado usuario, no fue posible procesar el contrato {numeroContrato}: {ex.Message}");
        }
    }

    private IQueryable<ContratoDb> Activos(string codigoEmpresa) =>
        _db.Contratos.AsNoTracking().Where(x => x.CodigoEmpresa == codigoEmpresa);

    private IQueryable<HistorialContratoDb> Retirados(string codigoEmpresa) =>
        _db.HistorialContratos.AsNoTracking().Where(x => x.CodigoEmpresa == codigoEmpresa && x.StatusHistorial == StatusRetiroCasa);

    private IQueryable<ContratoVencidoDatos> Proyectar(IQueryable<ContratoDb> contratos) =>
        from c in contratos
        join g in _db.Grupos.AsNoTracking()
            on new { c.CodigoEmpresa, c.CodigoGrupo } equals new { g.CodigoEmpresa, g.CodigoGrupo } into grupos
        from g in grupos.DefaultIfEmpty()
        join cl in _db.Clientes.AsNoTracking() on c.IdCliente equals cl.IdCliente into clientes
        from cl in clientes.DefaultIfEmpty()
        select new ContratoVencidoDatos(
            c.CodigoEmpresa, c.CodigoGrupo,
            g != null ? g.AbreviaturaGrupo : null, g != null ? g.DescripcionGrupo : null,
            c.NumeroContrato, c.Serie, c.IdCliente,
            cl != null ? cl.Nombre : null, cl != null ? cl.Apellido : null,
            c.FechaCreacion, c.FechaVencimiento, c.UltimaFechaPago,
            c.CapitalPrestado, c.SaldoActual, c.SaldoCapital, c.PlazoPago, c.Observacion, null);

    private IQueryable<ContratoVencidoDatos> Proyectar(IQueryable<HistorialContratoDb> historial) =>
        from h in historial
        join g in _db.Grupos.AsNoTracking()
            on new { h.CodigoEmpresa, h.CodigoGrupo } equals new { g.CodigoEmpresa, g.CodigoGrupo } into grupos
        from g in grupos.DefaultIfEmpty()
        join cl in _db.Clientes.AsNoTracking() on h.IdCliente equals cl.IdCliente into clientes
        from cl in clientes.DefaultIfEmpty()
        select new ContratoVencidoDatos(
            h.CodigoEmpresa, h.CodigoGrupo,
            g != null ? g.AbreviaturaGrupo : null, g != null ? g.DescripcionGrupo : null,
            h.NumeroContrato, null, h.IdCliente,
            cl != null ? cl.Nombre : null, cl != null ? cl.Apellido : null,
            h.FechaCreacion, h.FechaVencimiento, h.UltimaFechaPago,
            h.CapitalPrestado, h.Saldo, h.SaldoCapital, h.PlazoPago, null, h.FechaIngreso);
}
