using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class PagosReadService : IPagosReadService
{
    private readonly LegacyDataDbContext _dbContext;

    public PagosReadService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagosPendientesDto?> GetPagosPendientesAsync(string contratoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contratoId))
        {
            return null;
        }

        var id = contratoId.Trim();

        var contrato = await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => (x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim()) == id)
            .Select(x => new ContratoPagoDto(
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.FechaCreacion,
                x.Interes ?? 0m,
                x.InteresMensual ?? 0m,
                x.UltimaFechaPago,
                x.SaldoCapital ?? 0m,
                x.FechaVencimiento))
            .FirstOrDefaultAsync(cancellationToken);

        if (contrato is null)
        {
            return null;
        }

        return CalculadorPagosPendientes.Calcular(contrato);
    }
}