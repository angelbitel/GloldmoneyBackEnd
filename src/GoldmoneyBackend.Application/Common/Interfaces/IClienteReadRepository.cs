using GoldmoneyBackend.Application.Clientes.DTOs;

namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IClienteReadRepository
{
    Task<ClienteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClienteDto>> GetAllAsync(CancellationToken cancellationToken);
}