using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class SesionesRepository : ISesionesRepository
{
    // PARAMETROS_EMPRESA.cont_monto_caja: 1 = usar valor del dia anterior, 0 = usar monto inicial.
    private const decimal ManejoCajaInicioDia = 0m;
    private const decimal ManejoCajaDiaAnterior = 1m;

    private readonly LegacyDataDbContext _dbContext;

    public SesionesRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EstadoSesionEmpresaDto>> GetEstadoSesionesAsync(CancellationToken cancellationToken)
    {
        var empresas = await _dbContext.Empresas
            .AsNoTracking()
            .Where(x => x.ManejoCajaDep == 0)
            .OrderBy(x => x.NombreEmpresa)
            .ToListAsync(cancellationToken);

        var resultado = new List<EstadoSesionEmpresaDto>(empresas.Count);

        foreach (var empresa in empresas)
        {
            // Como en el VB6 (y en cobros y empeños): la última sesión está abierta mientras no
            // tenga hora de cierre. status_sesion = 1 marca las sesiones ya cerradas.
            var ultimaSesion = await _dbContext.SesionesEmpresa
                .AsNoTracking()
                .Where(x => x.CodigoEmpresa == empresa.CodigoEmpresa)
                .OrderByDescending(x => x.FechaApertura)
                .FirstOrDefaultAsync(cancellationToken);
            var tieneParametros = await _dbContext.ParametrosEmpresa
                .AsNoTracking()
                .AnyAsync(x => x.CodigoEmpresa == empresa.CodigoEmpresa, cancellationToken);

            resultado.Add(new EstadoSesionEmpresaDto(
                empresa.CodigoEmpresa,
                empresa.NombreEmpresa,
                ultimaSesion is { HoraCierre: null },
                ultimaSesion?.FechaApertura,
                tieneParametros));
        }

        return resultado;
    }

    public async Task<SesionAbiertaDto> AbrirSesionAsync(AbrirSesionDto dto, CancellationToken cancellationToken)
    {
        ValidateDto(dto);
        var codigoEmpresa = dto.CodigoEmpresa.Trim();

        var empresa = await _dbContext.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        if (empresa is null || empresa.ManejoCajaDep != 0)
        {
            throw new NotFoundDomainException("Empresa no encontrada o no habilitada para manejo de sesiones en tabla EMPRESA.");
        }

        var parametrosEmpresa = await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        if (parametrosEmpresa is null)
        {
            // SADE: "La empresa ... no cuenta con valor en caja para iniciar la sesión".
            throw new DomainValidationException(
                $"Estimado usuario, la empresa {empresa.NombreEmpresa?.Trim()} no tiene parámetros configurados (valor de caja, días de gracia); configúrelos en Empresas antes de abrir su caja.");
        }

        // Como el VB6: la sesión es del día local de la empresa, no de la fecha que mande el equipo.
        var hoy = OperacionesCajaLegacy.AhoraLocal().Date;

        return await OperacionesCajaLegacy.EnTransaccionAsync(_dbContext, async ct =>
        {
            // UPDLOCK + HOLDLOCK: dos aperturas simultáneas de la misma empresa se ejecutan una tras otra.
            var ultimaSesion = await _dbContext.SesionesEmpresa
                .FromSqlInterpolated($"""
                    SELECT TOP (1) * FROM INICIO_CIERRE_SESION WITH (UPDLOCK, HOLDLOCK)
                    WHERE codigo_empresa = {codigoEmpresa}
                    ORDER BY fecha_apertura DESC
                    """)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            // Una sola caja abierta por empresa: abierta = última sesión sin hora de cierre.
            if (ultimaSesion is { HoraCierre: null })
            {
                throw new ConflictDomainException(
                    $"La caja de la empresa ya está abierta (sesión del {ultimaSesion.FechaApertura:dd/MM/yyyy}). " +
                    "Debe cerrarse antes de abrir otra.");
            }

            // Una sesión por día y siempre posterior a la última (clave codigo_empresa + fecha_apertura).
            if (ultimaSesion is not null && ultimaSesion.FechaApertura.Date >= hoy)
            {
                throw new ConflictDomainException(
                    $"La empresa ya tuvo una sesión el {ultimaSesion.FechaApertura:dd/MM/yyyy}; la próxima caja se abre otro día.");
            }

            var valorInicial = CalcularValorCajaInicial(parametrosEmpresa.ContMontoCaja, ultimaSesion?.ValorFinalCaja, empresa);

            // sp_Gravar_Sesiones_Empresa: status 0 = abierta, sin hora ni usuario de cierre.
            _dbContext.SesionesEmpresa.Add(new IniciarCierreSesionDb
            {
                CodigoEmpresa = codigoEmpresa,
                FechaApertura = hoy,
                UsuarioApertura = dto.UsuarioResponsable,
                StatusSesion = 0,
                ValorInicialCaja = valorInicial
            });
            await _dbContext.SaveChangesAsync(ct);

            // También como sp_Gravar_Sesiones_Empresa: la caja de la empresa arranca en el valor inicial.
            await _dbContext.Empresas
                .Where(x => x.CodigoEmpresa == codigoEmpresa)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MontoAuxiliar, valorInicial), ct);

            await ActualizarSaldosDeLaEmpresaAsync(codigoEmpresa, ct);

            return new SesionAbiertaDto(codigoEmpresa, hoy, valorInicial);
        }, cancellationToken);
    }

    /// <summary>
    /// "Iniciar día" del VB6 (sp_Actualizar_Saldos_de_la_Empresa): recalcula saldo_actual e
    /// interés mensual de los contratos a la fecha de la sesión recién abierta. Se ejecuta el
    /// procedimiento original para que el resultado sea idéntico al del VB6.
    /// </summary>
    private async Task ActualizarSaldosDeLaEmpresaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var conexion = _dbContext.Database.GetDbConnection();
        await using var comando = conexion.CreateCommand();
        comando.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        comando.CommandText = "EXEC sp_Actualizar_Saldos_de_la_Empresa @codigo_empresa";

        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@codigo_empresa";
        parametro.Value = codigoEmpresa;
        comando.Parameters.Add(parametro);

        var estado = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
        if (estado != 0)
        {
            throw new DomainValidationException("No se pudieron actualizar los saldos de los contratos al abrir la caja.");
        }
    }

    public async Task<SesionCerradaDto> CerrarSesionAsync(string codigoEmpresa, string usuarioCierre, CancellationToken cancellationToken)
    {
        var codigo = codigoEmpresa.Trim();
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var empresa = await _dbContext.Empresas.FirstOrDefaultAsync(x => x.CodigoEmpresa == codigo, cancellationToken);
        if (empresa is null || empresa.ManejoCajaDep != 0)
        {
            throw new NotFoundDomainException("Empresa no encontrada o no habilitada para manejo de sesiones en tabla EMPRESA.");
        }

        var sesion = await _dbContext.SesionesEmpresa
            .Where(x => x.CodigoEmpresa == codigo && x.StatusSesion == EstadoSesion.Abierta)
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainValidationException("Estimado usuario, la caja de la empresa seleccionada no tiene una sesión abierta.");

        var ahora = await _dbContext.Database.SqlQueryRaw<DateTime>("SELECT GETDATE() AS \"Value\"").FirstAsync(cancellationToken);
        var fecha = sesion.FechaApertura.Date;
        var siguiente = fecha.AddDays(1);

        var movimientos = await _dbContext.MovimientosCaja
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigo && x.FechaTransaccion >= fecha && x.FechaTransaccion < siguiente)
            .GroupBy(x => x.CodigoTransaccion)
            .Select(g => new { Codigo = g.Key, Monto = g.Sum(x => x.MontoTransaccion ?? 0m) })
            .ToListAsync(cancellationToken);

        var egresos = movimientos.Where(m => CodigosEgreso.Contains(m.Codigo.Trim())).Sum(m => m.Monto);
        var ingresos = movimientos.Sum(m => m.Monto) - egresos;
        var valorFinal = empresa.MontoAuxiliar ?? 0m;
        var usuario = Recortar(usuarioCierre);

        // SADE guarda la hora sobre la fecha base 30/12/1899, igual que hora_transaccion.
        sesion.HoraCierre = new DateTime(1899, 12, 30).Add(ahora.TimeOfDay);
        sesion.UsuarioCierre = usuario;
        sesion.ValorFinalCaja = valorFinal;
        sesion.StatusSesion = EstadoSesion.Cerrada;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // MOVIMIENTO_TEMPORAL contiene los movimientos de la sesión en curso; se vacía al cerrar.
        var temporales = await _dbContext.MovimientosTemporales
            .Where(x => x.CodigoEmpresa == codigo)
            .ExecuteDeleteAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return new SesionCerradaDto(
            codigo, fecha, sesion.ValorInicialCaja ?? 0m, ingresos, egresos, valorFinal, ahora, usuario, temporales);
    }

    public async Task<ActualizacionSaldosDto> ActualizarSaldosAsync(string codigoEmpresaCaja, bool aplicar, CancellationToken cancellationToken)
    {
        var codigo = codigoEmpresaCaja.Trim();
        // Las empresas ligadas apuntan a la caja por EMPRESA.cod_empresa_caja (numérico).
        int? caja = int.TryParse(codigo, out var n) ? n : null;

        if (!await _dbContext.Empresas.AnyAsync(x => x.CodigoEmpresa == codigo, cancellationToken))
            throw new NotFoundDomainException("Empresa no encontrada.");

        // Fecha de trabajo de SADE: la última fecha de apertura de la caja.
        var fechaSistema = await _dbContext.SesionesEmpresa
            .Where(x => x.CodigoEmpresa == codigo)
            .MaxAsync(x => (DateTime?)x.FechaApertura, cancellationToken)
            ?? throw new DomainValidationException(
                "Estimado usuario, la caja de la empresa no tiene sesiones registradas; abra la caja antes de actualizar los saldos.");

        await using var tx = aplicar ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        // Misma fórmula que sp_Actualizar_Saldos_de_la_Empresa, evaluada con dbo.Meses_que_Debe.
        var filas = await _dbContext.Database.SqlQuery<SaldoEmpresaFila>($"""
            SELECT RTRIM(E.codigo_empresa) AS CodigoEmpresa, RTRIM(E.nombre_empresa) AS NombreEmpresa,
                   CAST(ISNULL(P.dg, 0) AS int) AS DiasGracia,
                   COUNT(C.numero_contrato) AS Contratos,
                   ISNULL(SUM(CASE WHEN C.numero_contrato IS NOT NULL
                       AND (ISNULL(C.saldo_actual, -1) <> X.nuevo OR ISNULL(C.interes_mensual, -1) <> X.mensual) THEN 1 ELSE 0 END), 0) AS ContratosConCambio,
                   CAST(ISNULL(SUM(C.saldo_actual), 0) AS decimal(18, 2)) AS SaldoAnterior,
                   CAST(ISNULL(SUM(X.nuevo), 0) AS decimal(18, 2)) AS SaldoNuevo
            FROM EMPRESA E
            OUTER APPLY (SELECT MAX(dias_gracia) AS dg FROM PARAMETROS_EMPRESA WHERE codigo_empresa = E.codigo_empresa) P
            LEFT JOIN CONTRATOS C ON C.codigo_empresa = E.codigo_empresa
            OUTER APPLY (SELECT
                CAST(C.saldo_capital + ((C.saldo_capital * (C.interes / 100))
                    * dbo.Meses_que_Debe(C.fecha_creacion, C.ultima_fecha_pago, {fechaSistema}, ISNULL(P.dg, 0))) AS money) AS nuevo,
                CAST(C.saldo_capital * (C.interes / 100) AS money) AS mensual) X
            WHERE E.cod_empresa_caja = {caja} OR E.codigo_empresa = {codigo}
            GROUP BY E.codigo_empresa, E.nombre_empresa, P.dg
            """).ToListAsync(cancellationToken);

        if (aplicar)
        {
            await AplicarSaldosAsync(codigo, caja, fechaSistema, cancellationToken);
            await tx!.CommitAsync(cancellationToken);
        }

        var empresas = filas
            .OrderBy(f => f.CodigoEmpresa)
            .Select(f => new ActualizacionSaldosEmpresaDto(
                f.CodigoEmpresa, f.NombreEmpresa, f.DiasGracia, f.Contratos, f.ContratosConCambio, f.SaldoAnterior, f.SaldoNuevo))
            .ToList();

        return new ActualizacionSaldosDto(
            codigo,
            fechaSistema,
            aplicar,
            empresas.Sum(e => e.Contratos),
            empresas.Sum(e => e.ContratosConCambio),
            empresas.Sum(e => e.SaldoAnterior),
            empresas.Sum(e => e.SaldoNuevo),
            empresas);
    }

    /// <summary>UPDATE de sp_Actualizar_Saldos_de_la_Empresa sobre la caja y sus empresas ligadas.</summary>
    private Task<int> AplicarSaldosAsync(string codigo, int? caja, DateTime fechaSistema, CancellationToken cancellationToken) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE C SET
                saldo_actual = C.saldo_capital + ((C.saldo_capital * (C.interes / 100))
                    * dbo.Meses_que_Debe(C.fecha_creacion, C.ultima_fecha_pago, {fechaSistema}, ISNULL(P.dg, 0))),
                interes_mensual = C.saldo_capital * (C.interes / 100)
            FROM CONTRATOS C
            JOIN EMPRESA E ON E.codigo_empresa = C.codigo_empresa
            OUTER APPLY (SELECT MAX(dias_gracia) AS dg FROM PARAMETROS_EMPRESA WHERE codigo_empresa = E.codigo_empresa) P
            WHERE E.cod_empresa_caja = {caja} OR E.codigo_empresa = {codigo}
            """, cancellationToken);

    private sealed record SaldoEmpresaFila(
        string CodigoEmpresa, string? NombreEmpresa, int DiasGracia, int Contratos, int ContratosConCambio,
        decimal SaldoAnterior, decimal SaldoNuevo);

    /// <summary>Códigos de MOVIMIENTO_CAJA que sacan dinero de la caja (EN, EA, RD); el resto son ingresos.</summary>
    private static readonly HashSet<string> CodigosEgreso = new(StringComparer.OrdinalIgnoreCase) { "EN", "EA", "RD" };

    /// <summary>usuario_apertura y usuario_cierre son char(14).</summary>
    private static string Recortar(string usuario)
    {
        var valor = usuario.Trim();
        return valor.Length > 14 ? valor[..14] : valor;
    }

    private static decimal CalcularValorCajaInicial(decimal? contMontoCaja, decimal? valorFinalUltimaSesion, EmpresaDb empresa)
    {
        return contMontoCaja switch
        {
            ManejoCajaDiaAnterior => valorFinalUltimaSesion is > 0 ? valorFinalUltimaSesion.Value : empresa.MontoAuxiliar ?? 0,
            ManejoCajaInicioDia => empresa.MontoInicial ?? 0,
            _ => throw new DomainValidationException("cont_monto_caja de PARAMETROS_EMPRESA tiene un valor no soportado.")
        };
    }

    private static void ValidateDto(AbrirSesionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoEmpresa))
        {
            throw new DomainValidationException("codigo_empresa es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioResponsable))
        {
            throw new DomainValidationException("usuario_responsable es obligatorio.");
        }
    }
}
