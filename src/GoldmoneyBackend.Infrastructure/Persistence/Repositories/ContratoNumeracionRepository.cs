using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ContratoNumeracionRepository : IContratoNumeracionRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public ContratoNumeracionRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProximoContratoDto> GetProximoAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var empresa = codigoEmpresa.Trim();

        var ultimaSerie = await _dbContext.Contratos
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa && x.CodigoGrupo == codigoGrupo
                && x.Serie != null && !x.NumeroContrato.StartsWith("WEB"))
            .OrderByDescending(x => x.Serie)
            .Select(x => x.Serie)
            .FirstOrDefaultAsync(cancellationToken);

        var proximoNumero = string.IsNullOrEmpty(ultimaSerie)
            ? 1
            : int.Parse(ultimaSerie) + 1;

        var serie = proximoNumero.ToString("D10");
        var numeroContrato = empresa + serie;

        return new ProximoContratoDto(numeroContrato, serie);
    }
}