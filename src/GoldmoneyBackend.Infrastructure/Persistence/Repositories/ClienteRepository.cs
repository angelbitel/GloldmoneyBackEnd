using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Entities.Clientes;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ClienteRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Clientes.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return query.AnyAsync(c => c.Email.Value == email, cancellationToken);
    }

    public Task<bool> ExistsByDocumentoAsync(string documento, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Clientes.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return query.AnyAsync(c => c.Documento.Value == documento, cancellationToken);
    }

    public Task AddAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        return _dbContext.Clientes.AddAsync(cliente, cancellationToken).AsTask();
    }

    public void Remove(Cliente cliente)
    {
        _dbContext.Clientes.Remove(cliente);
    }
}