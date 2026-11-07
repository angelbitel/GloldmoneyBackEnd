using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Operaciones de caja compartidas por empeños, abonos y retiros, escritas igual que el VB6
/// (<c>sp_GenerarSecuencia</c>, <c>sp_Actualizar_Caja_Empresa</c> y <c>sp_ad_Movimiento_Caja</c>).
/// El VB6 sigue operando sobre la misma base, así que la numeración sale de SECUENCIAS_EMPRESA
/// y nunca de <c>MAX()+1</c>, y la caja se modifica en la propia sentencia.
/// </summary>
internal static class OperacionesCajaLegacy
{
    public const string SecuenciaRecibos = "SR";
    public const string SecuenciaCaja = "SEC_CAJA";

    // El VB6 envía el texto 'NULL' como motivo de los movimientos ligados a un contrato.
    public const string MotivoMovimientoContrato = "NULL";

    // Hora local de la empresa: el VB6 graba la hora de la estación de trabajo.
    private static readonly TimeZoneInfo ZonaHorariaEmpresa = TimeZoneInfo.FindSystemTimeZoneById("America/Panama");

    public static DateTime AhoraLocal() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHorariaEmpresa);

    /// <summary>El VB6 envía solo la hora ("hh:mm:ss"), que SQL Server guarda sobre el 1900-01-01.</summary>
    public static DateTime SoloHora(DateTime ahora) =>
        new DateTime(1900, 1, 1).Add(new TimeSpan(ahora.Hour, ahora.Minute, ahora.Second));

    /// <summary>
    /// Incrementa y devuelve la secuencia en una sola sentencia, para no repetir números
    /// con el VB6 (sp_GenerarSecuencia). Si no existe la crea con 1, como el original.
    /// </summary>
    public static async Task<int> SiguienteSecuenciaAsync(LegacyDataDbContext dbContext, string codigoEmpresa, string codigoSecuencia, CancellationToken cancellationToken)
    {
        var valores = await dbContext.Database
            .SqlQuery<int>($"""
                UPDATE SECUENCIAS_EMPRESA SET secuencia_actual = ISNULL(secuencia_actual, 0) + 1
                OUTPUT inserted.secuencia_actual AS Value
                WHERE LTRIM(RTRIM(codigo_empresa)) = {codigoEmpresa} AND LTRIM(RTRIM(codigo_secuencia)) = {codigoSecuencia}
                """)
            .ToListAsync(cancellationToken);

        if (valores.Count > 1)
        {
            throw new InvalidOperationException($"La secuencia '{codigoSecuencia}' de la empresa '{codigoEmpresa}' está duplicada en SECUENCIAS_EMPRESA.");
        }

        if (valores.Count == 1)
        {
            return valores[0];
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO SECUENCIAS_EMPRESA (codigo_empresa, codigo_secuencia, secuencia_actual) VALUES ({codigoEmpresa}, {codigoSecuencia}, 1)",
            cancellationToken);
        return 1;
    }

    /// <summary>
    /// Suma (o resta, con un monto negativo) en la propia sentencia para no pisar un movimiento
    /// simultáneo del VB6. Con <paramref name="exigirSaldo"/> falla si la caja no alcanza.
    /// </summary>
    public static async Task ActualizarCajaEmpresaAsync(LegacyDataDbContext dbContext, string codigoEmpresa, decimal monto, CancellationToken cancellationToken, bool exigirSaldo = false)
    {
        var filas = exigirSaldo
            ? await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE EMPRESA SET monto_auxiliar = monto_auxiliar + {monto} WHERE codigo_empresa = {codigoEmpresa} AND (monto_auxiliar IS NULL OR monto_auxiliar + {monto} >= 0)",
                cancellationToken)
            : await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE EMPRESA SET monto_auxiliar = monto_auxiliar + {monto} WHERE codigo_empresa = {codigoEmpresa}",
                cancellationToken);

        if (filas != 1)
        {
            throw new InvalidOperationException($"No se pudo actualizar la caja de la empresa '{codigoEmpresa}'.");
        }
    }

    /// <summary>
    /// Movimiento en MOVIMIENTO_CAJA y su copia en MOVIMIENTO_TEMPORAL (sp_ad_Movimiento_Caja),
    /// numerado con SEC_CAJA. Queda pendiente de <c>SaveChangesAsync</c>.
    /// </summary>
    public static async Task<int> AgregarMovimientoAsync(
        LegacyDataDbContext dbContext,
        string codigoEmpresa,
        int codigoGrupo,
        string numeroContrato,
        string codigoTransaccion,
        decimal monto,
        DateTime fechaTransaccion,
        DateTime ahora,
        string usuarioResponsable,
        CancellationToken cancellationToken,
        string motivo = MotivoMovimientoContrato)
    {
        var numeroMovimiento = await SiguienteSecuenciaAsync(dbContext, codigoEmpresa, SecuenciaCaja, cancellationToken);
        var hora = SoloHora(ahora);

        dbContext.MovimientosCaja.Add(new MovimientoCajaDb
        {
            CodigoEmpresa = codigoEmpresa,
            NumeroMovimiento = numeroMovimiento,
            CodigoTransaccion = codigoTransaccion,
            CodigoGrupo = codigoGrupo,
            NumeroContrato = numeroContrato,
            MontoTransaccion = monto,
            FechaTransaccion = fechaTransaccion,
            HoraTransaccion = hora,
            MotivoTransaccion = motivo,
            UsuarioResponsable = usuarioResponsable
        });

        dbContext.MovimientosTemporales.Add(new MovimientoTemporalDb
        {
            CodigoEmpresa = codigoEmpresa,
            NumeroMovimiento = numeroMovimiento,
            CodigoTransaccion = codigoTransaccion,
            CodigoGrupo = codigoGrupo,
            NumeroContrato = numeroContrato,
            MontoTransaccion = monto,
            FechaTransaccion = fechaTransaccion,
            HoraTransaccion = hora,
            MotivoTransaccion = motivo,
            UsuarioResponsable = usuarioResponsable
        });

        return numeroMovimiento;
    }

    /// <summary>
    /// Ejecuta <paramref name="operacion"/> en una transacción. Si falla, limpia el ChangeTracker
    /// para que las entidades pendientes no se graben con la operación siguiente.
    /// </summary>
    public static async Task<T> EnTransaccionAsync<T>(LegacyDataDbContext dbContext, Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken)
    {
        await using var transaccion = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var resultado = await operacion(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    /// <summary>
    /// Fecha de la sesión abierta de la empresa y sus días de gracia. La sesión abierta es la
    /// última y la única sin hora de cierre; no se usa status_sesion porque el VB6 y
    /// SesionesRepository lo interpretan al revés.
    /// </summary>
    public static async Task<(DateTime? FechaSesion, int DiasGracia)> GetSesionAbiertaAsync(LegacyDataDbContext dbContext, string codigoEmpresa, CancellationToken cancellationToken)
    {
        var ultimaSesion = await dbContext.SesionesEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .OrderByDescending(x => x.FechaApertura)
            .Select(x => new { x.FechaApertura, x.HoraCierre })
            .FirstOrDefaultAsync(cancellationToken);

        var diasGracia = await dbContext.ParametrosEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .MaxAsync(x => x.DiasGracia, cancellationToken);

        var fechaSesion = ultimaSesion is { HoraCierre: null } ? ultimaSesion.FechaApertura.Date : (DateTime?)null;
        return (fechaSesion, (int)(diasGracia ?? 0m));
    }

    /// <summary>Contrato bloqueado (UPDLOCK) para que dos cobros simultáneos se ejecuten uno tras otro.</summary>
    public static Task<ContratoDb?> GetContratoBloqueadoAsync(LegacyDataDbContext dbContext, string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken) =>
        dbContext.Contratos
            .FromSqlInterpolated($"""
                SELECT * FROM CONTRATOS WITH (UPDLOCK, ROWLOCK)
                WHERE codigo_empresa = {codigoEmpresa} AND codigo_grupo = {codigoGrupo} AND numero_contrato = {numeroContrato}
                """)
            .FirstOrDefaultAsync(cancellationToken);
}
