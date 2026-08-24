namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IClientesDataService
{
    Task CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
}
