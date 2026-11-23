using GoldmoneyBackend.Application.Caja;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Authentication;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reescritura en EF Core de los procedimientos de SADE: sp_ad_Copia_Recibo, sp_ad_Historial_Contrato,
/// sp_ad_Efectuar_Anulacion_Contrato, sp_ad_Movimiento_Caja y sp_Actualizar_Caja_Empresa.
/// </summary>
public sealed class TransaccionesContratoRepository : ITransaccionesContratoRepository
{
    private const string TransaccionCopiaRecibo = "CR";
    private const string TransaccionAnulacion = "AC";
    private const string SecuenciaCopia = "CR";
    private const string SecuenciaRecibo = "SR";

    private readonly LegacyDataDbContext _db;
    private readonly AuthOptions _authOptions;

    public TransaccionesContratoRepository(LegacyDataDbContext db, IOptions<AuthOptions> authOptions)
    {
        _db = db;
        _authOptions = authOptions.Value;
    }

    public async Task<IReadOnlyList<ContratoTransaccionDatos>> GetContratosPorCedulaAsync(string cedula, CancellationToken cancellationToken)
    {
        var contratos = await QueryContratos()
            .Where(x => x.Contrato.IdCliente == cedula)
            .OrderBy(x => x.Contrato.NumeroContrato)
            .ThenBy(x => x.AbreviaturaGrupo)
            .ToListAsync(cancellationToken);

        return await CompletarAsync(contratos, _db.Contratos.Where(x => x.IdCliente == cedula), cancellationToken);
    }

    public async Task<ContratoTransaccionDatos?> GetContratoAsync(ContratoClave clave, CancellationToken cancellationToken)
    {
        var contratos = await QueryContratos()
            .Where(x => x.Contrato.CodigoEmpresa == clave.CodigoEmpresa
                && x.Contrato.CodigoGrupo == clave.CodigoGrupo
                && x.Contrato.NumeroContrato == clave.NumeroContrato)
            .ToListAsync(cancellationToken);

        var filtro = _db.Contratos.Where(x => x.CodigoEmpresa == clave.CodigoEmpresa
            && x.CodigoGrupo == clave.CodigoGrupo
            && x.NumeroContrato == clave.NumeroContrato);
        var completos = await CompletarAsync(contratos, filtro, cancellationToken);
        return completos.FirstOrDefault();
    }

    public async Task<ParametrosTransaccionDto?> GetParametrosAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var p = await _db.ParametrosEmpresa
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        return p is null
            ? null
            : new ParametrosTransaccionDto(p.CostoCopia, p.ControlarAnulacion == 1, p.AnulacionControladaTiempo == 1, p.TiempoAnulacion);
    }

    public async Task<SesionOperacionDto?> GetSesionOperacionAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var codigoCaja = await ResolverEmpresaCajaAsync(codigoEmpresa, cancellationToken);
        if (codigoCaja is null) return null;

        var fecha = await _db.SesionesEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoCaja && x.StatusSesion == EstadoSesion.Abierta)
            .MaxAsync(x => (DateTime?)x.FechaApertura, cancellationToken);

        return fecha is null ? null : new SesionOperacionDto(codigoCaja, fecha.Value);
    }

    public Task<DateTime> GetFechaServidorAsync(CancellationToken cancellationToken) =>
        _db.Database.SqlQueryRaw<DateTime>("SELECT GETDATE() AS \"Value\"").FirstAsync(cancellationToken);

    public async Task<bool> ValidarSupervisorAnulacionAsync(string usuario, string clave, CancellationToken cancellationToken)
    {
        var candidatos = await _db.Usuarios
            .AsNoTracking()
            .Where(x => x.NombreUsuario == usuario && x.EfectuarAnulacion == 1)
            .Select(x => x.Contrasena)
            .ToListAsync(cancellationToken);

        return candidatos.Any(c => PasswordHasher.Verify(clave, c.Trim()))
            || (candidatos.Count == 0 && ValidarUsuarioConfigurado(usuario, clave, requiereAdmin: true));
    }

    /// <summary>
    /// Usuarios definidos en Auth:Users (appsettings), que no existen en USUARIOS: se validan igual que en el
    /// inicio de sesión. Si se exige administrador, el usuario debe tener el rol Admin.
    /// </summary>
    private bool ValidarUsuarioConfigurado(string usuario, string clave, bool requiereAdmin)
    {
        var configurado = _authOptions.Users.FirstOrDefault(u =>
            string.Equals(u.UserName, usuario.Trim(), StringComparison.OrdinalIgnoreCase));
        return configurado is not null
            && (!requiereAdmin || configurado.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            && PasswordHasher.Verify(clave, configurado.Password);
    }

    public async Task<bool> ActualizarObservacionAsync(ContratoClave clave, string? observacion, CancellationToken cancellationToken)
    {
        var filas = await _db.Contratos
            .Where(x => x.CodigoEmpresa == clave.CodigoEmpresa
                && x.CodigoGrupo == clave.CodigoGrupo
                && x.NumeroContrato == clave.NumeroContrato)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Observacion, observacion), cancellationToken);

        return filas == 1;
    }

    public async Task<CopiaReciboRegistradaDto> RegistrarCopiaReciboAsync(RegistrarCopiaReciboDatos datos, CancellationToken cancellationToken)
    {
        var c = datos.Contrato;
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var numeroCopia = await SiguienteSecuenciaAsync(c.CodigoEmpresa, SecuenciaCopia, cancellationToken);
        var secuenciaRecibo = await SiguienteSecuenciaAsync(c.CodigoEmpresa, SecuenciaRecibo, cancellationToken);

        _db.CopiasRecibo.Add(new CopiaReciboDb
        {
            CodigoEmpresa = c.CodigoEmpresa,
            NumeroCopia = numeroCopia,
            CodigoGrupo = c.CodigoGrupo,
            NumeroContrato = c.NumeroContrato,
            IdCliente = datos.IdCliente,
            Fecha = datos.FechaOperacion,
            Hora = datos.Hora,
            Capital = datos.Monto,
            UsuarioResponsable = datos.UsuarioResponsable,
            SecuenciaRecibo = secuenciaRecibo
        });

        await IncrementarCajaAsync(datos.CodigoEmpresaCaja, datos.Monto, cancellationToken);
        await AgregarMovimientoAsync(c, TransaccionCopiaRecibo, datos.Monto, datos.FechaOperacion, datos.Hora, "COPIA DE RECIBO", datos.UsuarioResponsable, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CopiaReciboRegistradaDto(numeroCopia, secuenciaRecibo);
    }

    public async Task AnularContratoAsync(AnularContratoDatos datos, CancellationToken cancellationToken)
    {
        var c = datos.Contrato;
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var contrato = await _db.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == c.CodigoEmpresa
                && x.CodigoGrupo == c.CodigoGrupo
                && x.NumeroContrato == c.NumeroContrato, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el número de contrato no existe en la base de datos.");

        // sp_ad_Efectuar_Anulacion_Contrato: no se anula si existen pagos (revalidado dentro de la transacción).
        var tienePagos = await _db.Pagos.AnyAsync(x => x.CodigoEmpresa == c.CodigoEmpresa
            && x.CodigoGrupo == c.CodigoGrupo
            && x.NumeroContrato == c.NumeroContrato, cancellationToken);
        if (tienePagos)
            throw new DomainValidationException($"Estimado usuario, el contrato [{contrato.Serie?.Trim()}] tiene registrados abonos a interés, por lo que no es posible anularlo.");

        var yaEnHistorial = await _db.HistorialContratos.AnyAsync(x => x.CodigoEmpresa == c.CodigoEmpresa
            && x.CodigoGrupo == c.CodigoGrupo
            && x.NumeroContrato == c.NumeroContrato, cancellationToken);
        if (yaEnHistorial)
            throw new ConflictDomainException("Estimado usuario, el contrato ya se encuentra registrado en el historial.");

        // Proyección con categoría nullable: los contratos antiguos tienen codigo_categoria_prenda NULL.
        var detalles = await _db.DetallesContratos
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == c.CodigoEmpresa && x.CodigoGrupo == c.CodigoGrupo && x.NumeroContrato == c.NumeroContrato)
            .Select(x => new
            {
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.SecuenciaContratos,
                x.Descripcion,
                x.Kilates,
                x.Peso,
                x.CodigoReloj,
                x.CantidadProducto,
                CodigoCategoriaPrenda = (int?)x.CodigoCategoriaPrenda
            })
            .ToListAsync(cancellationToken);

        // sp_ad_Historial_Contrato + inserción de H_DETALLES_CONTRATOS.
        _db.HistorialContratos.Add(new HistorialContratoDb
        {
            CodigoEmpresa = c.CodigoEmpresa,
            CodigoGrupo = c.CodigoGrupo,
            NumeroContrato = c.NumeroContrato,
            StatusHistorial = TransaccionAnulacion,
            IdCliente = contrato.IdCliente,
            FechaCreacion = contrato.FechaCreacion,
            FechaVencimiento = contrato.FechaVencimiento,
            CapitalPrestado = contrato.CapitalPrestado,
            TasaInteres = contrato.Interes.HasValue ? Math.Round(contrato.Interes.Value, 0) : null,
            InteresMensual = contrato.InteresMensual,
            PlazoPago = contrato.PlazoPago,
            UltimaFechaPago = contrato.UltimaFechaPago,
            Saldo = contrato.SaldoActual,
            SaldoCapital = contrato.SaldoCapital,
            UsuarioResponsable = datos.UsuarioResponsable,
            FechaIngreso = datos.FechaOperacion,
            HoraIngreso = datos.Hora,
            UsuarioCreacion = contrato.UsuarioResponsable,
            HoraCreacion = contrato.HoraTransaccion,
            MontoMaximo = contrato.MontoMaximo
        });

        _db.HDetallesContratos.AddRange(detalles.Select(d => new HDetalleContratoDb
        {
            CodigoEmpresa = d.CodigoEmpresa,
            CodigoGrupo = d.CodigoGrupo,
            NumeroContrato = d.NumeroContrato,
            NumeroDetalle = d.SecuenciaContratos,
            Descripcion = d.Descripcion?.Trim(),
            Kilates = d.Kilates,
            Peso = d.Peso,
            CodigoReloj = d.CodigoReloj,
            CantidadProducto = d.CantidadProducto,
            CodigoCategoriaPrenda = d.CodigoCategoriaPrenda
        }));

        // El dinero prestado regresa a la caja.
        var capital = contrato.CapitalPrestado ?? 0m;
        await IncrementarCajaAsync(datos.CodigoEmpresaCaja, capital, cancellationToken);
        await AgregarMovimientoAsync(c, TransaccionAnulacion, capital, datos.FechaOperacion, datos.Hora, "ANULACION CONTRATO", datos.UsuarioResponsable, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        // Se elimina el movimiento de creación (EN/EA) y el contrato; las FK en cascada
        // eliminan DETALLES_CONTRATOS, COPIA_RECIBO, PAGOS, DESCUENTOS_INTERES y PRORROGA_VENCIMIENTO.
        await _db.MovimientosCaja
            .Where(x => x.CodigoEmpresa == c.CodigoEmpresa
                && x.CodigoGrupo == c.CodigoGrupo
                && x.NumeroContrato == c.NumeroContrato
                && (x.CodigoTransaccion == "EN" || x.CodigoTransaccion == "EA"))
            .ExecuteDeleteAsync(cancellationToken);

        var eliminados = await _db.Contratos
            .Where(x => x.CodigoEmpresa == c.CodigoEmpresa
                && x.CodigoGrupo == c.CodigoGrupo
                && x.NumeroContrato == c.NumeroContrato)
            .ExecuteDeleteAsync(cancellationToken);

        if (eliminados != 1)
            throw new ConflictDomainException("Estimado usuario, ocurrió un error inesperado al momento de efectuar la anulación del contrato.");

        await tx.CommitAsync(cancellationToken);
    }

    public Task<string?> GetEmpresaCajaAsync(string codigoEmpresa, CancellationToken cancellationToken) =>
        ResolverEmpresaCajaAsync(codigoEmpresa, cancellationToken);

    public async Task<bool> ValidarCredencialCajaAsync(string usuarioConectado, string usuario, string clave, CancellationToken cancellationToken)
    {
        var candidatos = await _db.Usuarios
            .AsNoTracking()
            .Where(x => x.NombreUsuario == usuario)
            .Select(x => new { x.NombreUsuario, x.Contrasena, x.UsuarioAdmin })
            .ToListAsync(cancellationToken);

        var esConectado = string.Equals(usuario.Trim(), usuarioConectado?.Trim(), StringComparison.OrdinalIgnoreCase);
        if (candidatos.Count == 0)
            return ValidarUsuarioConfigurado(usuario, clave, requiereAdmin: !esConectado);

        return candidatos.Any(c => (esConectado || c.UsuarioAdmin == 1) && PasswordHasher.Verify(clave, c.Contrasena.Trim()));
    }

    public async Task<MovimientoCajaRegistradoDto> RegistrarMovimientoManualAsync(MovimientoCajaManualDatos datos, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var empresa = await _db.Empresas.FirstOrDefaultAsync(x => x.CodigoEmpresa == datos.CodigoEmpresaCaja, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, la empresa seleccionada no contiene un monto actual.");

        // Se revalida dentro de la transacción con el monto vigente.
        var anterior = empresa.MontoAuxiliar ?? 0m;
        ReglasMovimientoCaja.ValidarFondos(datos.Tipo, datos.Monto, anterior);

        var numero = await AgregarMovimientoAsync(
            datos.CodigoEmpresaCaja, null, null, datos.Tipo, datos.Monto, datos.FechaSesion, datos.Hora,
            datos.Motivo, datos.UsuarioResponsable, cancellationToken);

        var nuevo = anterior + (datos.Tipo == TipoMovimientoManual.Retiro ? -datos.Monto : datos.Monto);
        empresa.MontoAuxiliar = nuevo;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new MovimientoCajaRegistradoDto(numero, anterior, nuevo);
    }

    public async Task<IReadOnlyList<MovimientoCajaRecienteDto>> GetMovimientosRecientesAsync(
        string codigoEmpresaCaja, string tipo, int cantidad, CancellationToken cancellationToken) =>
        await _db.MovimientosCaja
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresaCaja && x.CodigoTransaccion == tipo)
            .OrderByDescending(x => x.NumeroMovimiento)
            .Take(cantidad)
            .Select(x => new MovimientoCajaRecienteDto(
                x.NumeroMovimiento, x.MontoTransaccion ?? 0m, x.FechaTransaccion, x.HoraTransaccion, x.MotivoTransaccion, x.UsuarioResponsable))
            .ToListAsync(cancellationToken);

    private IQueryable<ContratoProyeccion> QueryContratos() =>
        from contrato in _db.Contratos.AsNoTracking()
        join empresa in _db.Empresas.AsNoTracking() on contrato.CodigoEmpresa equals empresa.CodigoEmpresa into empresas
        from empresa in empresas.DefaultIfEmpty()
        join grupo in _db.Grupos.AsNoTracking()
            on new { contrato.CodigoEmpresa, contrato.CodigoGrupo } equals new { grupo.CodigoEmpresa, grupo.CodigoGrupo } into grupos
        from grupo in grupos.DefaultIfEmpty()
        select new ContratoProyeccion
        {
            Contrato = contrato,
            NombreEmpresa = empresa != null ? empresa.NombreEmpresa : null,
            AbreviaturaGrupo = grupo != null ? grupo.AbreviaturaGrupo : null,
            CantidadCopias = _db.CopiasRecibo.Count(x => x.CodigoEmpresa == contrato.CodigoEmpresa
                && x.CodigoGrupo == contrato.CodigoGrupo
                && x.NumeroContrato == contrato.NumeroContrato),
            TienePagos = _db.Pagos.Any(x => x.CodigoEmpresa == contrato.CodigoEmpresa
                && x.CodigoGrupo == contrato.CodigoGrupo
                && x.NumeroContrato == contrato.NumeroContrato)
        };

    /// <param name="filtro">Mismos contratos como IQueryable; se usa un join en lugar de Contains(lista)
    /// porque la BD legacy tiene un nivel de compatibilidad sin OPENJSON.</param>
    private async Task<IReadOnlyList<ContratoTransaccionDatos>> CompletarAsync(
        List<ContratoProyeccion> contratos, IQueryable<ContratoDb> filtro, CancellationToken cancellationToken)
    {
        if (contratos.Count == 0) return [];

        var detalles = await (
            from c in filtro.AsNoTracking()
            join d in _db.DetallesContratos.AsNoTracking()
                on new { c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato } equals new { d.CodigoEmpresa, d.CodigoGrupo, d.NumeroContrato }
            join cat in _db.CategoriasPrenda.AsNoTracking() on d.CodigoCategoriaPrenda equals cat.CodigoCategoriaPrenda into cats
            from cat in cats.DefaultIfEmpty()
            orderby d.SecuenciaContratos
            select new
            {
                d.CodigoEmpresa,
                d.CodigoGrupo,
                d.NumeroContrato,
                d.SecuenciaContratos,
                d.Descripcion,
                d.Kilates,
                d.Peso,
                d.CantidadProducto,
                Categoria = cat != null ? cat.NombreCategoriaPrenda : null
            })
            .ToListAsync(cancellationToken);

        return contratos.Select(x =>
        {
            var c = x.Contrato;
            var items = detalles
                .Where(d => d.CodigoEmpresa.Trim() == c.CodigoEmpresa.Trim()
                    && d.CodigoGrupo == c.CodigoGrupo
                    && d.NumeroContrato == c.NumeroContrato)
                .Select(d => new DetalleTransaccionDto(
                    d.SecuenciaContratos, d.Descripcion?.Trim(), d.Kilates, d.Peso, d.CantidadProducto, d.Categoria?.Trim()))
                .ToList();

            return new ContratoTransaccionDatos(
                c.CodigoEmpresa, x.NombreEmpresa, c.CodigoGrupo, x.AbreviaturaGrupo, c.NumeroContrato, c.Serie, c.IdCliente,
                c.CapitalPrestado ?? 0m, c.FechaCreacion, c.Interes, c.PlazoPago, c.FechaVencimiento, c.InteresMensual,
                c.SaldoCapital, c.Observacion, c.HoraTransaccion, c.UsuarioResponsable, x.CantidadCopias, x.TienePagos, items);
        }).ToList();
    }

    /// <summary>EMPRESA.manejo_caja_dep = 1 indica que la caja la maneja cod_empresa_caja.</summary>
    private async Task<string?> ResolverEmpresaCajaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var empresa = await _db.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        if (empresa is null) return null;

        return empresa.ManejoCajaDep == 1 && empresa.CodEmpresaCaja.HasValue
            ? empresa.CodEmpresaCaja.Value.ToString()
            : empresa.CodigoEmpresa.Trim();
    }

    private async Task IncrementarCajaAsync(string codigoEmpresaCaja, decimal monto, CancellationToken cancellationToken)
    {
        var empresa = await _db.Empresas.FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresaCaja, cancellationToken)
            ?? throw new DomainValidationException("Estimado usuario, el sistema no pudo actualizar la cantidad de dinero en caja.");

        empresa.MontoAuxiliar = (empresa.MontoAuxiliar ?? 0m) + monto;
    }

    private Task<int> AgregarMovimientoAsync(
        ContratoClave c, string codigoTransaccion, decimal monto, DateTime fecha, DateTime hora,
        string motivo, string usuario, CancellationToken cancellationToken) =>
        AgregarMovimientoAsync(c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato, codigoTransaccion, monto, fecha, hora, motivo, usuario, cancellationToken);

    private async Task<int> AgregarMovimientoAsync(
        string codigoEmpresa, int? codigoGrupo, string? numeroContrato, string codigoTransaccion, decimal monto,
        DateTime fecha, DateTime hora, string motivo, string usuario, CancellationToken cancellationToken)
    {
        // Mismo criterio que LegacyEmpenioRepository: MAX(numero_movimiento) + 1 por empresa.
        var ultimo = await _db.MovimientosCaja
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .MaxAsync(x => (int?)x.NumeroMovimiento, cancellationToken) ?? 0;
        var numero = ultimo + 1;

        // sp_ad_Movimiento_Caja inserta en MOVIMIENTO_CAJA y MOVIMIENTO_TEMPORAL.
        _db.MovimientosCaja.Add(new MovimientoCajaDb
        {
            CodigoEmpresa = codigoEmpresa,
            NumeroMovimiento = numero,
            CodigoTransaccion = codigoTransaccion,
            CodigoGrupo = codigoGrupo,
            NumeroContrato = numeroContrato,
            MontoTransaccion = monto,
            FechaTransaccion = fecha,
            HoraTransaccion = hora,
            MotivoTransaccion = motivo,
            UsuarioResponsable = usuario
        });

        _db.MovimientosTemporales.Add(new MovimientoTemporalDb
        {
            CodigoEmpresa = codigoEmpresa,
            NumeroMovimiento = numero,
            CodigoTransaccion = codigoTransaccion,
            CodigoGrupo = codigoGrupo,
            NumeroContrato = numeroContrato,
            MontoTransaccion = monto,
            FechaTransaccion = fecha,
            HoraTransaccion = hora,
            MotivoTransaccion = motivo,
            UsuarioResponsable = usuario
        });

        return numero;
    }

    /// <summary>Equivale a Generar_Secuencia de SADE sobre SECUENCIAS_EMPRESA, sin guardar (lo hace la transacción).</summary>
    private async Task<int> SiguienteSecuenciaAsync(string codigoEmpresa, string codigoSecuencia, CancellationToken cancellationToken)
    {
        var secuencia = await _db.SecuenciasEmpresa
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa && x.CodigoSecuencia == codigoSecuencia, cancellationToken);

        if (secuencia is null)
        {
            secuencia = new SecuenciaEmpresaDb { CodigoEmpresa = codigoEmpresa, CodigoSecuencia = codigoSecuencia, SecuenciaActual = 0 };
            _db.SecuenciasEmpresa.Add(secuencia);
        }

        secuencia.SecuenciaActual++;
        return secuencia.SecuenciaActual;
    }

    private sealed class ContratoProyeccion
    {
        public ContratoDb Contrato { get; init; } = null!;
        public string? NombreEmpresa { get; init; }
        public string? AbreviaturaGrupo { get; init; }
        public int CantidadCopias { get; init; }
        public bool TienePagos { get; init; }
    }
}
