using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Historial;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Lecturas de historial: pagos de un contrato (PAGOS + HISTORIAL_PAGOS), contratos que ya salieron
/// de CONTRATOS (HISTORIAL_CONTRATOS) y métricas de cartera.
/// </summary>
public sealed class HistorialReadService : IHistorialReadService
{
    private const string StatusRetirado = "RC";
    private const string StatusRematado = "CF";
    private const string StatusAnulado = "AC";

    private readonly LegacyDataDbContext _db;

    public HistorialReadService(LegacyDataDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PagoHistorialDto>> GetPagosContratoAsync(
        string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        var empresa = codigoEmpresa.Trim();
        var contrato = numeroContrato.Trim();

        var filas = await _db.Database.SqlQuery<PagoFila>($"""
            SELECT numero_pagos AS NumeroPagos, fecha_pago AS FechaPago, cantidad_pagos AS CantidadPagos,
                   pago_interes AS PagoInteres, pago_capital AS PagoCapital, saldo_capital AS SaldoCapital,
                   ultima_fecha_pago AS UltimaFechaPago, secuencia_recibo AS SecuenciaRecibo,
                   usuario_responsable AS UsuarioResponsable
            FROM PAGOS
            WHERE RTRIM(codigo_empresa) = {empresa} AND codigo_grupo = {codigoGrupo} AND RTRIM(numero_contrato) = {contrato}
            UNION ALL
            SELECT numero_pagos, fecha_pago, cantidad_pagos, pago_interes, pago_capital, saldo_capital,
                   ultima_fecha_pago, secuencia_recibo, usuario_responsable
            FROM HISTORIAL_PAGOS
            WHERE RTRIM(codigo_empresa) = {empresa} AND codigo_grupo = {codigoGrupo} AND RTRIM(numero_contrato) = {contrato}
            """).ToListAsync(cancellationToken);

        return filas
            .OrderByDescending(x => x.FechaPago)
            .ThenByDescending(x => x.NumeroPagos)
            .Select(x => new PagoHistorialDto(
                x.NumeroPagos, x.FechaPago, x.CantidadPagos ?? 0, x.PagoInteres ?? 0, x.PagoCapital ?? 0,
                x.SaldoCapital ?? 0, x.UltimaFechaPago, x.SecuenciaRecibo ?? 0, x.UsuarioResponsable?.Trim()))
            .ToList();
    }

    public async Task<HistorialClienteDto> GetHistorialClienteAsync(string cedula, CancellationToken cancellationToken)
    {
        var id = cedula.Trim();

        var contratos = await _db.HistorialContratos.AsNoTracking()
            .Where(x => x.IdCliente != null && x.IdCliente.Trim() == id)
            .ToListAsync(cancellationToken);

        if (contratos.Count == 0)
        {
            return new HistorialClienteDto([], [], []);
        }

        var totales = await _db.Database.SqlQuery<TotalPagosFila>($"""
            SELECT RTRIM(h.codigo_empresa) AS CodigoEmpresa, h.codigo_grupo AS CodigoGrupo,
                   RTRIM(h.numero_contrato) AS NumeroContrato,
                   CAST(ISNULL(SUM(h.cantidad_pagos), 0) AS int) AS Cuotas,
                   CAST(ISNULL(SUM(h.pago_interes), 0) AS decimal(18, 2)) AS Interes,
                   CAST(ISNULL(SUM(h.pago_capital), 0) AS decimal(18, 2)) AS Capital
            FROM HISTORIAL_PAGOS h
            INNER JOIN HISTORIAL_CONTRATOS c
                ON c.codigo_empresa = h.codigo_empresa AND c.codigo_grupo = h.codigo_grupo
                AND c.numero_contrato = h.numero_contrato
            WHERE RTRIM(c.id_cliente) = {id}
            GROUP BY RTRIM(h.codigo_empresa), h.codigo_grupo, RTRIM(h.numero_contrato)
            """).ToListAsync(cancellationToken);

        var porContrato = totales.ToDictionary(t => (t.CodigoEmpresa, t.CodigoGrupo, t.NumeroContrato));

        var dtos = contratos
            .Select(c =>
            {
                porContrato.TryGetValue(
                    (c.CodigoEmpresa.Trim(), c.CodigoGrupo, c.NumeroContrato.Trim()), out var t);
                return new ContratoHistorialDto(
                    c.CodigoEmpresa.Trim(), c.CodigoGrupo, c.NumeroContrato.Trim(), c.StatusHistorial.Trim(),
                    c.FechaCreacion, c.FechaVencimiento, c.FechaIngreso, c.UltimaFechaPago,
                    c.CapitalPrestado ?? 0, c.InteresMensual ?? 0, c.SaldoCapital ?? 0, c.Saldo ?? 0,
                    t?.Cuotas ?? 0, t?.Interes ?? 0, t?.Capital ?? 0);
            })
            .OrderByDescending(x => x.FechaSalida)
            .ToList();

        return new HistorialClienteDto(
            dtos.Where(x => x.Status == StatusRetirado).ToList(),
            dtos.Where(x => x.Status == StatusRematado).ToList(),
            dtos.Where(x => x.Status == StatusAnulado).ToList());
    }

    public async Task<MetricasCarteraDto> GetMetricasAsync(
        string codigoEmpresa, string? cedula, CancellationToken cancellationToken)
    {
        var empresa = codigoEmpresa.Trim();
        var id = string.IsNullOrWhiteSpace(cedula) ? null : cedula.Trim();

        var fechaSesion = (await _db.SesionesEmpresa.AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa)
            .MaxAsync(x => (DateTime?)x.FechaApertura, cancellationToken) ?? DateTime.Today).Date;

        var activos = _db.Contratos.AsNoTracking().Where(x => x.CodigoEmpresa == empresa);
        var historial = _db.HistorialContratos.AsNoTracking().Where(x => x.CodigoEmpresa == empresa);
        if (id is not null)
        {
            activos = activos.Where(x => x.IdCliente.Trim() == id);
            historial = historial.Where(x => x.IdCliente != null && x.IdCliente.Trim() == id);
        }

        var total = await activos.CountAsync(cancellationToken);
        var vencidos = await activos
            .CountAsync(x => x.FechaVencimiento != null && x.FechaVencimiento < fechaSesion, cancellationToken);
        var capital = await activos.SumAsync(x => x.SaldoCapital ?? 0, cancellationToken);
        var saldo = await activos.SumAsync(x => x.SaldoActual ?? 0, cancellationToken);

        var porStatus = await historial
            .GroupBy(x => x.StatusHistorial)
            .Select(g => new { Status = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        int Contar(string status) => porStatus.Where(x => x.Status.Trim() == status).Sum(x => x.Total);

        var interes = Math.Max(0, saldo - capital);

        return new MetricasCarteraDto(
            total, vencidos, capital, interes, capital + interes,
            Contar(StatusRetirado), Contar(StatusRematado), Contar(StatusAnulado), fechaSesion);
    }

    private sealed class PagoFila
    {
        public int NumeroPagos { get; set; }
        public DateTime FechaPago { get; set; }
        public int? CantidadPagos { get; set; }
        public decimal? PagoInteres { get; set; }
        public decimal? PagoCapital { get; set; }
        public decimal? SaldoCapital { get; set; }
        public DateTime? UltimaFechaPago { get; set; }
        public int? SecuenciaRecibo { get; set; }
        public string? UsuarioResponsable { get; set; }
    }

    private sealed class TotalPagosFila
    {
        public string CodigoEmpresa { get; set; } = string.Empty;
        public int CodigoGrupo { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public int Cuotas { get; set; }
        public decimal Interes { get; set; }
        public decimal Capital { get; set; }
    }
}
