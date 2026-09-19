using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ClientesRepository : IClientesRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public ClientesRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Clientes.AsNoTracking();

        if (TryNormalizePanamaId(search, out var normalizedIdCliente))
        {
            query = query.Where(x => x.IdCliente == normalizedIdCliente);
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            var textSearch = search.Trim();
            query = query.Where(x =>
                (x.Nombre != null && x.Nombre.Contains(textSearch))
                || (x.Apellido != null && x.Apellido.Contains(textSearch)));
        }

        return await query
            .OrderBy(x => x.IdCliente)
            .Select(x => new ClienteDbDto(
                x.IdCliente,
                x.Apellido,
                x.Nombre,
                x.Telefono,
                x.Estatus,
                x.Direccion,
                x.Comentario,
                x.CodigoPais,
                x.CodigoProvincia,
                x.CodigoDistrito,
                x.CodigoCorregimiento))
            .ToListAsync(cancellationToken);
    }

    private static bool TryNormalizePanamaId(string? value, out string normalizedIdCliente)
    {
        normalizedIdCliente = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length == 3 && parts.All(part => part.Length > 0 && part.All(char.IsDigit)))
        {
            if (parts[0].Length <= 3 && parts[1].Length <= 4 && parts[2].Length <= 5)
            {
                normalizedIdCliente = string.Join('-',
                    parts[0].PadLeft(3, '0'),
                    parts[1].PadLeft(4, '0'),
                    parts[2].PadLeft(5, '0'));
                return true;
            }

            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 7)
        {
            normalizedIdCliente = $"{digits[..1].PadLeft(3, '0')}-"
                + $"{digits[1..3].PadLeft(4, '0')}-"
                + $"{digits[3..].PadLeft(5, '0')}";
            return true;
        }

        if (digits.Length == 12)
        {
            normalizedIdCliente = $"{digits[..3]}-{digits[3..7]}-{digits[7..]}";
            return true;
        }

        return false;
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
