using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Empenios;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ContratoNumeracionRepository : IContratoNumeracionRepository
{
    private const int MaxIntentos = 1000;

    private readonly LegacyDataDbContext _dbContext;

    public ContratoNumeracionRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Número que probablemente recibirá el próximo contrato del grupo, solo para mostrarlo.
    /// No toca SECUENCIAS_EMPRESA: el número definitivo se reserva al guardar el contrato,
    /// y puede ser otro si mientras tanto el VB6 u otro cajero creó un contrato.
    /// </summary>
    public async Task<ProximoContratoDto> GetProximoAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var empresa = codigoEmpresa.Trim();

        // La abreviatura del grupo es el código de su secuencia en SECUENCIAS_EMPRESA.
        var abreviatura = await _dbContext.Grupos
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa && x.CodigoGrupo == codigoGrupo)
            .Select(x => x.AbreviaturaGrupo)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(abreviatura))
            throw new InvalidOperationException(
                $"No se encontró el grupo {codigoGrupo} para la empresa '{empresa}' o no tiene abreviatura definida.");

        var codigoSecuencia = abreviatura.Trim();
        var actual = await _dbContext.SecuenciasEmpresa
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == empresa && x.CodigoSecuencia == codigoSecuencia)
            .Select(x => (int?)x.SecuenciaActual)
            .FirstOrDefaultAsync(cancellationToken);

        // Igual que al guardar: la secuencia se incrementa y se usa el nuevo valor.
        var secuencia = (actual ?? 0) + 1;

        for (var intento = 0; intento < MaxIntentos; intento++, secuencia++)
        {
            var (numeroContrato, serie) = CalculoEmpenio.Numerar(empresa, codigoGrupo, secuencia);

            var existe = await _dbContext.Contratos
                .AsNoTracking()
                .AnyAsync(x => x.CodigoEmpresa == empresa
                    && x.CodigoGrupo == codigoGrupo
                    && x.NumeroContrato == numeroContrato, cancellationToken);

            if (!existe)
            {
                return new ProximoContratoDto(numeroContrato, serie);
            }
        }

        throw new InvalidOperationException(
            $"No se pudo encontrar un número de contrato disponible después de {MaxIntentos} intentos " +
            $"para la empresa '{empresa}' y grupo {codigoGrupo}.");
    }
}
