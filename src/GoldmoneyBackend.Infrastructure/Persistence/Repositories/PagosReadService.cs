using System.Linq.Expressions;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Pagos;
using GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class PagosReadService : IPagosReadService
{
    private readonly LegacyDataDbContext _dbContext;

    public PagosReadService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContratoPagoDto?> GetContratoAsync(string contratoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contratoId))
        {
            return null;
        }

        var id = contratoId.Trim();

        var contrato = await ContratosConGrupo(x => x.NumeroContrato.Trim() == id
                || (x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim()) == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (contrato is null)
        {
            return null;
        }

        var (fechaSesion, diasGracia) = await GetContextoEmpresaAsync(contrato.Contrato.CodigoEmpresa, cancellationToken);
        return ToDto(contrato, fechaSesion, diasGracia);
    }

    public async Task<IReadOnlyList<ContratoPagoDto>> GetContratosClienteAsync(string idCliente, CancellationToken cancellationToken)
    {
        var cedula = idCliente.Trim();

        var contratos = await ContratosConGrupo(x => x.IdCliente == cedula)
            .ToListAsync(cancellationToken);

        var contextos = new Dictionary<string, (DateTime FechaSesion, int DiasGracia)>();
        var resultado = new List<ContratoPagoDto>(contratos.Count);

        foreach (var contrato in contratos)
        {
            var codigoEmpresa = contrato.Contrato.CodigoEmpresa.Trim();
            if (!contextos.TryGetValue(codigoEmpresa, out var contexto))
            {
                contexto = await GetContextoEmpresaAsync(codigoEmpresa, cancellationToken);
                contextos[codigoEmpresa] = contexto;
            }

            resultado.Add(ToDto(contrato, contexto.FechaSesion, contexto.DiasGracia));
        }

        return resultado;
    }

    public async Task<ConfiguracionCobroDto?> GetConfiguracionCobroAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var empresa = await _dbContext.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        if (empresa is null)
        {
            return null;
        }

        var parametros = await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa, cancellationToken);

        return new ConfiguracionCobroDto(
            empresa.CodigoEmpresa.Trim(),
            empresa.NombreEmpresa?.Trim(),
            empresa.Direccion?.Trim(),
            empresa.Telefono?.Trim(),
            empresa.Ruc?.Trim(),
            parametros?.RestarAbonoCapital == 1m,
            parametros?.ImprimirReciboPago == 1m,
            parametros?.OperarEtiquetas == 1m,
            Math.Max(parametros?.NoEtiquetasPagos ?? 0, 0),
            parametros?.ImprimirEtiquetaRetiro == 1,
            Math.Max(parametros?.NoEtiquetasRetiros ?? 0, 0));
    }

    /// <summary>
    /// Contratos activos con la abreviatura de su grupo. Como en el VB6, se cruza con
    /// GRUPOS: un contrato cuyo grupo no exista no aparece para cobro. El filtro y el orden
    /// se aplican antes de proyectar para que EF Core pueda traducirlos a SQL.
    /// </summary>
    private IQueryable<ContratoConGrupo> ContratosConGrupo(Expression<Func<ContratoDb, bool>> filtro) =>
        from contrato in _dbContext.Contratos.AsNoTracking().Where(filtro)
        join grupo in _dbContext.Grupos.AsNoTracking()
            on new { contrato.CodigoEmpresa, contrato.CodigoGrupo } equals new { grupo.CodigoEmpresa, grupo.CodigoGrupo }
        orderby contrato.NumeroContrato, grupo.AbreviaturaGrupo
        select new ContratoConGrupo(contrato, grupo.AbreviaturaGrupo);

    /// <summary>
    /// Fecha de trabajo y días de gracia de la empresa. La fecha de trabajo es la de la
    /// última sesión registrada, igual que <c>sp_Actualizar_Saldos_de_la_Empresa</c>
    /// (en el legacy la sesión abierta, <c>status_sesion = 0</c>, es siempre la última).
    /// </summary>
    private async Task<(DateTime FechaSesion, int DiasGracia)> GetContextoEmpresaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var codigo = codigoEmpresa.Trim();

        var fechaSesion = await _dbContext.SesionesEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigo)
            .MaxAsync(x => (DateTime?)x.FechaApertura, cancellationToken)
            ?? DateTime.Today;

        var diasGracia = await _dbContext.ParametrosEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigo)
            .MaxAsync(x => x.DiasGracia, cancellationToken);

        return (fechaSesion.Date, (int)(diasGracia ?? 0m));
    }

    private static ContratoPagoDto ToDto(ContratoConGrupo fila, DateTime fechaSesion, int diasGracia)
    {
        var c = fila.Contrato;

        return new ContratoPagoDto(
            c.CodigoEmpresa,
            c.CodigoGrupo,
            fila.AbreviaturaGrupo,
            c.NumeroContrato,
            c.IdCliente,
            c.FechaCreacion,
            c.CapitalPrestado ?? 0m,
            c.Interes ?? 0m,
            c.SaldoCapital ?? 0m,
            c.UltimaFechaPago,
            c.FechaVencimiento,
            c.PlazoPago ?? 0,
            fechaSesion,
            diasGracia);
    }

    private sealed record ContratoConGrupo(ContratoDb Contrato, string? AbreviaturaGrupo);
}
