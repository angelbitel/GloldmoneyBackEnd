namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IClientesRepository
{
    Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(string? search, CancellationToken cancellationToken);
    Task CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
}
