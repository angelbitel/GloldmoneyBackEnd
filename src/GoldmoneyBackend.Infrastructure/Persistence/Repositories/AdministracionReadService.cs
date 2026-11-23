using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class AdministracionReadService : IAdministracionReadService
{
    /// <summary>
    /// Códigos de MOVIMIENTO_CAJA (Ingresar_Movimiento_Caja de SADE). Egreso = sale dinero de caja.
    /// </summary>
    private static readonly Dictionary<string, (string Descripcion, string Tipo)> CodigosCaja = new()
    {
        ["EN"] = ("Empeños nuevos", "Egreso"),
        ["EA"] = ("Empeños activos", "Egreso"),
        ["RD"] = ("Retiro de dinero de caja", "Egreso"),
        ["AI"] = ("Abono a interés", "Ingreso"),
        ["AP"] = ("Abono parcial", "Ingreso"),
        ["RC"] = ("Retiro de contrato", "Ingreso"),
        ["MR"] = ("Retiro de prendas (capital)", "Ingreso"),
        ["IR"] = ("Retiro de prendas (interés)", "Ingreso"),
        ["CR"] = ("Copia de recibo", "Ingreso"),
        ["AC"] = ("Anulación de contrato", "Ingreso"),
        ["DD"] = ("Depósito de dinero en caja", "Ingreso"),
    };

    private readonly LegacyDataDbContext _db;

    public AdministracionReadService(LegacyDataDbContext db)
    {
        _db = db;
    }

    public async Task<InventarioDto> GetInventarioAsync(string codigoEmpresa, int? codigoGrupo, string? buscar, CancellationToken cancellationToken)
    {
        var empresa = codigoEmpresa.Trim();
        var hoy = DateTime.Today;

        var query =
            from c in _db.Contratos.AsNoTracking()
            where c.CodigoEmpresa == empresa
            join cl in _db.Clientes.AsNoTracking() on c.IdCliente equals cl.IdCliente into cls
            from cl in cls.DefaultIfEmpty()
            join g in _db.Grupos.AsNoTracking()
                on new { c.CodigoEmpresa, c.CodigoGrupo } equals new { g.CodigoEmpresa, g.CodigoGrupo } into gs
            from g in gs.DefaultIfEmpty()
            select new { c, cl, g };

        if (codigoGrupo.HasValue)
            query = query.Where(x => x.c.CodigoGrupo == codigoGrupo.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(x =>
                x.c.NumeroContrato.Contains(texto)
                || (x.c.Serie != null && x.c.Serie.Contains(texto))
                || x.c.IdCliente.Contains(texto)
                || (x.cl != null && ((x.cl.Nombre != null && x.cl.Nombre.Contains(texto))
                    || (x.cl.Apellido != null && x.cl.Apellido.Contains(texto)))));
        }

        // Agregados de las prendas por subconsulta (sin Contains(lista): la BD no soporta OPENJSON).
        var filas = await query
            .OrderBy(x => x.c.FechaVencimiento)
            .Select(x => new
            {
                x.c.CodigoEmpresa,
                x.c.CodigoGrupo,
                AbreviaturaGrupo = x.g != null ? x.g.AbreviaturaGrupo : null,
                x.c.NumeroContrato,
                x.c.Serie,
                x.c.IdCliente,
                Nombre = x.cl != null ? x.cl.Nombre : null,
                Apellido = x.cl != null ? x.cl.Apellido : null,
                x.c.FechaCreacion,
                x.c.FechaVencimiento,
                x.c.CapitalPrestado,
                x.c.SaldoCapital,
                Prendas = _db.DetallesContratos.Count(d => d.CodigoEmpresa == x.c.CodigoEmpresa
                    && d.CodigoGrupo == x.c.CodigoGrupo && d.NumeroContrato == x.c.NumeroContrato),
                Peso = _db.DetallesContratos.Where(d => d.CodigoEmpresa == x.c.CodigoEmpresa
                    && d.CodigoGrupo == x.c.CodigoGrupo && d.NumeroContrato == x.c.NumeroContrato).Sum(d => d.Peso),
                Kilates = _db.DetallesContratos.Where(d => d.CodigoEmpresa == x.c.CodigoEmpresa
                    && d.CodigoGrupo == x.c.CodigoGrupo && d.NumeroContrato == x.c.NumeroContrato).Max(d => d.Kilates),
            })
            .ToListAsync(cancellationToken);

        var items = filas.Select(f => new InventarioItemDto(
                f.CodigoEmpresa.Trim(),
                f.CodigoGrupo,
                f.AbreviaturaGrupo?.Trim().ToUpperInvariant(),
                f.NumeroContrato.Trim(),
                f.Serie?.Trim(),
                f.IdCliente.Trim(),
                string.Join(' ', new[] { f.Nombre?.Trim(), f.Apellido?.Trim() }.Where(s => !string.IsNullOrEmpty(s))),
                f.FechaCreacion,
                f.FechaVencimiento,
                f.CapitalPrestado ?? 0m,
                f.SaldoCapital ?? 0m,
                f.Prendas,
                f.Peso ?? 0m,
                f.Kilates,
                f.FechaVencimiento.HasValue && f.FechaVencimiento.Value.Date < hoy))
            .ToList();

        return new InventarioDto(
            items.Count,
            items.Sum(i => i.CapitalPrestado),
            items.Sum(i => i.SaldoCapital),
            items.Sum(i => i.PesoTotal),
            items.Count(i => i.Vencido),
            items);
    }

    public async Task<EstadoCajaDto?> GetEstadoCajaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var codigo = codigoEmpresa.Trim();
        var empresa = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.CodigoEmpresa == codigo, cancellationToken);
        if (empresa is null) return null;

        // EMPRESA.manejo_caja_dep = 1: la caja la maneja cod_empresa_caja (Establecer_Datos_Empresa_en_Manejo).
        var codigoCaja = empresa.ManejoCajaDep == 1 && empresa.CodEmpresaCaja.HasValue
            ? empresa.CodEmpresaCaja.Value.ToString()
            : empresa.CodigoEmpresa.Trim();

        var empresaCaja = codigoCaja == empresa.CodigoEmpresa.Trim()
            ? empresa
            : await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoCaja, cancellationToken) ?? empresa;

        var sesion = await _db.SesionesEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoCaja && x.StatusSesion == EstadoSesion.Abierta)
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(cancellationToken);

        var movimientos = new List<MovimientoCajaResumenDto>();
        if (sesion is not null)
        {
            var fecha = sesion.FechaApertura.Date;
            var siguiente = fecha.AddDays(1);
            var agrupados = await _db.MovimientosCaja
                .AsNoTracking()
                .Where(x => x.CodigoEmpresa == codigoCaja && x.FechaTransaccion >= fecha && x.FechaTransaccion < siguiente)
                .GroupBy(x => x.CodigoTransaccion)
                .Select(g => new { Codigo = g.Key, Cantidad = g.Count(), Monto = g.Sum(x => x.MontoTransaccion) ?? 0m })
                .ToListAsync(cancellationToken);

            movimientos = agrupados
                .Select(g =>
                {
                    var codigoTx = g.Codigo.Trim();
                    var (descripcion, tipo) = CodigosCaja.TryGetValue(codigoTx, out var info) ? info : ($"Movimiento {codigoTx}", "Ingreso");
                    return new MovimientoCajaResumenDto(codigoTx, descripcion, tipo, g.Cantidad, g.Monto);
                })
                .OrderBy(m => m.Tipo)
                .ThenByDescending(m => m.Monto)
                .ToList();
        }

        int? cajaNumero = int.TryParse(codigoCaja, out var n) ? n : null;
        var ligadas = cajaNumero is null
            ? new List<EmpresaLigadaDto>()
            : (await _db.Empresas
                .AsNoTracking()
                .Where(x => x.CodEmpresaCaja == cajaNumero && x.ManejoCajaDep == 1)
                .Select(x => new { x.CodigoEmpresa, x.NombreEmpresa })
                .ToListAsync(cancellationToken))
                .Where(x => x.CodigoEmpresa.Trim() != codigoCaja)
                .OrderBy(x => x.CodigoEmpresa)
                .Select(x => new EmpresaLigadaDto(x.CodigoEmpresa.Trim(), x.NombreEmpresa?.Trim()))
                .ToList();

        return new EstadoCajaDto(
            empresa.CodigoEmpresa.Trim(),
            empresa.NombreEmpresa?.Trim(),
            codigoCaja,
            sesion is not null,
            sesion?.FechaApertura,
            sesion?.ValorInicialCaja,
            empresaCaja.MontoInicial,
            empresaCaja.MontoAuxiliar ?? 0m,
            movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto),
            movimientos.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto),
            movimientos,
            ligadas);
    }
}
