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

        // Buscar el siguiente número disponible que no exista ya en la base de datos
        const int maxAttempts = 1000;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var serie = proximoNumero.ToString("D10");
            var numeroContrato = empresa + serie;

            var existe = await _dbContext.Contratos
                .AsNoTracking()
                .AnyAsync(x => x.CodigoEmpresa == empresa
                    && x.CodigoGrupo == codigoGrupo
                    && x.NumeroContrato == numeroContrato, cancellationToken);

            if (!existe)
            {
                return new ProximoContratoDto(numeroContrato, serie);
            }

            proximoNumero++;
        }

        throw new InvalidOperationException(
            $"No se pudo encontrar un número de contrato disponible después de {maxAttempts} intentos " +
            $"para la empresa '{empresa}' y grupo {codigoGrupo}.");
    }
}