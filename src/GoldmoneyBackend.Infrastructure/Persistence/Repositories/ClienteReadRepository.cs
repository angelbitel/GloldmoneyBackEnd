using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ClienteReadRepository : IClienteReadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ClienteReadRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClienteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteDto(
                c.Id,
                c.Nombre,
                c.Email.Value,
                c.Documento.Value,
                c.Estado.ToString(),
                c.FechaCreacion,
                c.FechaActualizacion))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClienteDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes
            .AsNoTracking()
            .OrderByDescending(c => c.FechaCreacion)
            .Select(c => new ClienteDto(
                c.Id,
                c.Nombre,
                c.Email.Value,
                c.Documento.Value,
                c.Estado.ToString(),
                c.FechaCreacion,
                c.FechaActualizacion))
            .ToListAsync(cancellationToken);
    }
}