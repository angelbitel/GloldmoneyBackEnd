using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ClientesDataService : IClientesDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public ClientesDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.IdCliente, "id_cliente");

        var exists = await _dbContext.Clientes
            .AsNoTracking()
            .AnyAsync(x => x.IdCliente == dto.IdCliente, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un cliente con ese id_cliente.");
        }

        var entity = new ClienteDb
        {
            IdCliente = dto.IdCliente.Trim(),
            Apellido = TrimOrNull(dto.Apellido),
            Nombre = TrimOrNull(dto.Nombre),
            Telefono = TrimOrNull(dto.Telefono),
            Estatus = dto.Estatus,
            Direccion = TrimOrNull(dto.Direccion),
            Comentario = TrimOrNull(dto.Comentario),
            CodigoPais = TrimOrNull(dto.CodigoPais),
            CodigoProvincia = TrimOrNull(dto.CodigoProvincia),
            CodigoDistrito = TrimOrNull(dto.CodigoDistrito),
            CodigoCorregimiento = TrimOrNull(dto.CodigoCorregimiento)
        };

        await _dbContext.Clientes.AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo crear el cliente en tabla CLIENTES. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateKey(dto.IdCliente, "id_cliente");

        var entity = await _dbContext.Clientes
            .FirstOrDefaultAsync(x => x.IdCliente == dto.IdCliente, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Cliente no encontrado en tabla CLIENTES.");
        }

        entity.Apellido = TrimOrNull(dto.Apellido);
        entity.Nombre = TrimOrNull(dto.Nombre);
        entity.Telefono = TrimOrNull(dto.Telefono);
        entity.Estatus = dto.Estatus;
        entity.Direccion = TrimOrNull(dto.Direccion);
        entity.Comentario = TrimOrNull(dto.Comentario);
        entity.CodigoPais = TrimOrNull(dto.CodigoPais);
        entity.CodigoProvincia = TrimOrNull(dto.CodigoProvincia);
        entity.CodigoDistrito = TrimOrNull(dto.CodigoDistrito);
        entity.CodigoCorregimiento = TrimOrNull(dto.CodigoCorregimiento);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo actualizar el cliente en tabla CLIENTES. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void ValidateKey(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} es obligatorio.");
        }
    }
}
