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

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TOP 1
                RTRIM(codigo_empresa) + CAST(codigo_grupo AS varchar(10)) + RTRIM(numero_contrato) AS contrato_id,
                codigo_empresa,
                codigo_grupo,
                numero_contrato,
                id_cliente,
                fecha_creacion,
                capital_prestado,
                saldo_capital,
                usuario_responsable
            FROM CONTRATOS
            WHERE RTRIM(codigo_empresa) + CAST(codigo_grupo AS varchar(10)) + RTRIM(numero_contrato) = @contrato_id";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@contrato_id";
        parameter.Value = contratoId.Trim();
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ContratoDto(
            reader.GetString(0).Trim(),
            reader.GetString(1).Trim(),
            reader.GetInt32(2),
            reader.GetString(3).Trim(),
            reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
            reader.GetDateTime(5),
            Convert.ToDecimal(reader.GetValue(6)),
            Convert.ToDecimal(reader.GetValue(7)),
            reader.IsDBNull(8) ? null : reader.GetString(8).Trim());
    }

    public async Task<IReadOnlyList<ContratoDto>> GetContratosByCedulaAsync(string cedula, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return Array.Empty<ContratoDto>();
        }

        var contratos = new List<ContratoDto>();
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                RTRIM(codigo_empresa) + CAST(codigo_grupo AS varchar(10)) + RTRIM(numero_contrato) AS contrato_id,
                codigo_empresa,
                codigo_grupo,
                numero_contrato,
                id_cliente,
                fecha_creacion,
                capital_prestado,
                saldo_capital,
                usuario_responsable
            FROM CONTRATOS
            WHERE id_cliente = @id_cliente
            ORDER BY fecha_creacion DESC";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id_cliente";
        parameter.Value = cedula.Trim();
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            contratos.Add(new ContratoDto(
                reader.GetString(0).Trim(),
                reader.GetString(1).Trim(),
                reader.GetInt32(2),
                reader.GetString(3).Trim(),
                reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
                reader.GetDateTime(5),
                Convert.ToDecimal(reader.GetValue(6)),
                Convert.ToDecimal(reader.GetValue(7)),
                reader.IsDBNull(8) ? null : reader.GetString(8).Trim()));
        }

        return contratos;
    }
}
