using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class EmpeniosReadService : IEmpeniosReadService
{
    private readonly LegacyDataDbContext _dbContext;

    public EmpeniosReadService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContratoDto?> GetContratoByIdAsync(string contratoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contratoId))
        {
            return null;
        }

        var id = contratoId.Trim();

        return await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => (x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim()) == id)
            .Select(x => new ContratoDto(
                x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim(),
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.IdCliente,
                x.FechaCreacion ?? DateTime.MinValue,
                x.CapitalPrestado ?? 0m,
                x.SaldoCapital ?? 0m,
                x.UsuarioResponsable))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContratoDto>> GetContratosByCedulaAsync(string cedula, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return Array.Empty<ContratoDto>();
        }

        return await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => x.IdCliente == cedula.Trim())
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new ContratoDto(
                x.CodigoEmpresa.Trim() + x.CodigoGrupo.ToString() + x.NumeroContrato.Trim(),
                x.CodigoEmpresa,
                x.CodigoGrupo,
                x.NumeroContrato,
                x.IdCliente,
                x.FechaCreacion ?? DateTime.MinValue,
                x.CapitalPrestado ?? 0m,
                x.SaldoCapital ?? 0m,
                x.UsuarioResponsable))
            .ToListAsync(cancellationToken);
    }
}
