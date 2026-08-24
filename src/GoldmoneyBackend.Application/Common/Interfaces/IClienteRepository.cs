using GoldmoneyBackend.Domain.Entities.Clientes;

namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsByDocumentoAsync(string documento, Guid? excludeId, CancellationToken cancellationToken);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken);
    void Remove(Cliente cliente);
}