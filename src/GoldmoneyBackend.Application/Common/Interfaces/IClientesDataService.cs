namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IClientesDataService
{
    Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
}
