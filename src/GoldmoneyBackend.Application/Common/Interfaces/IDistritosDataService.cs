namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IDistritosDataService
{
    Task<IReadOnlyList<DistritoDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<DistritoDbDto?> GetByKeyAsync(string codigoDistrito, CancellationToken cancellationToken);
    Task CreateAsync(DistritoDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string codigoDistrito, DistritoDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string codigoDistrito, CancellationToken cancellationToken);
}
