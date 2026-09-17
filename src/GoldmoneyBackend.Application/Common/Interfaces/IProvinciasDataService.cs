namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IProvinciasDataService
{
    Task<IReadOnlyList<ProvinciaDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProvinciaDbDto?> GetByKeyAsync(string codigoProvincia, CancellationToken cancellationToken);
    Task CreateAsync(ProvinciaDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string codigoProvincia, ProvinciaDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string codigoProvincia, CancellationToken cancellationToken);
}
