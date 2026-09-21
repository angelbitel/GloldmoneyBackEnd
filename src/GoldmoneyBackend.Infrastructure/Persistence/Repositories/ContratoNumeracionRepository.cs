using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
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

        // Obtener la abreviatura del grupo desde GRUPOS para usarla como CodigoSecuencia en SECUENCIAS_EMPRESA
        var grupo = await _dbContext.Grupos
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa && x.CodigoGrupo == codigoGrupo)
            .Select(x => new { x.AbreviaturaGrupo })
            .FirstOrDefaultAsync(cancellationToken);

        if (grupo is null || string.IsNullOrWhiteSpace(grupo.AbreviaturaGrupo))
            throw new InvalidOperationException(
                $"No se encontró el grupo {codigoGrupo} para la empresa '{empresa}' o no tiene abreviatura definida.");

        var abreviatura = grupo.AbreviaturaGrupo.Trim();

        // Obtener el siguiente número desde SECUENCIAS_EMPRESA usando la abreviatura del grupo como CodigoSecuencia
        var proximoNumero = await ObtenerProximaSecuenciaAsync(empresa, abreviatura, cancellationToken);

        // Buscar el siguiente número disponible que no exista ya en la base de datos
        const int maxAttempts = 1000;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var serie = proximoNumero.ToString("D10");
            var numeroContrato = empresa + codigoGrupo.ToString() + serie;

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

    private async Task<int> ObtenerProximaSecuenciaAsync(string empresa, string codigoSecuencia, CancellationToken cancellationToken)
    {
        var secuencia = await _dbContext.SecuenciasEmpresa
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == empresa && x.CodigoSecuencia == codigoSecuencia, cancellationToken);

        if (secuencia is null)
        {
            secuencia = new SecuenciaEmpresaDb
            {
                CodigoEmpresa = empresa,
                CodigoSecuencia = codigoSecuencia,
                SecuenciaActual = 1
            };
            _dbContext.SecuenciasEmpresa.Add(secuencia);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return 1;
        }

        secuencia.SecuenciaActual++;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return secuencia.SecuenciaActual;
    }
}