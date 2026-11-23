using GoldmoneyBackend.Application.Clientes;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class ClientesRepository : IClientesRepository
{
    /// <summary>Máximo de clientes que devuelve una búsqueda (el buscador muestra los primeros).</summary>
    private const int LimiteBusqueda = 50;

    private readonly LegacyDataDbContext _dbContext;

    public ClientesRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Clientes.AsNoTracking();

        if (string.IsNullOrWhiteSpace(search))
        {
            return await query
                .OrderBy(x => x.IdCliente)
                .Select(AClienteDbDto)
                .ToListAsync(cancellationToken);
        }

        var (filtrada, cedula) = FiltrarPorBusqueda(query, search);
        query = filtrada;

        // Coincidencia exacta de cédula primero; el buscador solo necesita los primeros resultados.
        return await query
            .OrderBy(x => x.IdCliente == cedula ? 0 : 1)
            .ThenBy(x => x.Nombre)
            .ThenBy(x => x.Apellido)
            .Take(LimiteBusqueda)
            .Select(AClienteDbDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientesPaginadoDto> GetPaginadoAsync(
        string? buscar, FiltroClientes filtro, int pagina, int tamanoPagina, CancellationToken cancellationToken)
    {
        (pagina, tamanoPagina) = PaginacionClientes.Normalizar(pagina, tamanoPagina);
        var clientes = _dbContext.Clientes.AsNoTracking();
        var contratos = _dbContext.Contratos.AsNoTracking();

        var (query, cedula) = FiltrarPorBusqueda(clientes, buscar);
        query = filtro switch
        {
            FiltroClientes.Activos => query.Where(x => x.Estatus == 1),
            FiltroClientes.Inactivos => query.Where(x => x.Estatus != 1),
            FiltroClientes.ConContratos => query.Where(x => contratos.Any(c => c.IdCliente == x.IdCliente && c.SaldoCapital > 0)),
            _ => query
        };

        var total = await query.CountAsync(cancellationToken);
        var filas = await query
            .OrderBy(x => x.IdCliente == cedula ? 0 : 1)
            .ThenBy(x => x.Apellido)
            .ThenBy(x => x.Nombre)
            .ThenBy(x => x.IdCliente)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(x => new ClienteListadoDto(
                x.IdCliente.Trim(),
                x.Apellido,
                x.Nombre,
                x.Telefono,
                x.Estatus,
                x.Direccion,
                contratos.Count(c => c.IdCliente == x.IdCliente && c.SaldoCapital > 0),
                contratos.Where(c => c.IdCliente == x.IdCliente && c.SaldoCapital > 0).Sum(c => c.SaldoCapital) ?? 0m))
            .ToListAsync(cancellationToken);

        var totalClientes = await clientes.CountAsync(cancellationToken);
        var activos = await clientes.CountAsync(x => x.Estatus == 1, cancellationToken);
        var conContratos = await clientes.CountAsync(
            x => contratos.Any(c => c.IdCliente == x.IdCliente && c.SaldoCapital > 0), cancellationToken);

        return new ClientesPaginadoDto(
            total, pagina, tamanoPagina,
            new ResumenClientesDto(totalClientes, activos, totalClientes - activos, conContratos),
            filas);
    }

    /// <summary>
    /// Cédula (completa, parcial o en formato normalizado), nombre, apellido o nombre completo.
    /// Devuelve también la cédula normalizada para ordenar primero la coincidencia exacta.
    /// </summary>
    private static (IQueryable<ClienteDb> Query, string? Cedula) FiltrarPorBusqueda(IQueryable<ClienteDb> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return (query, null);

        var texto = search.Trim();
        var cedula = TryNormalizePanamaId(texto, out var normalizada) ? normalizada : texto;
        return (query.Where(x =>
            x.IdCliente == cedula
            || x.IdCliente.Contains(texto)
            || (x.Nombre != null && x.Nombre.Contains(texto))
            || (x.Apellido != null && x.Apellido.Contains(texto))
            || ((x.Nombre ?? "").Trim() + " " + (x.Apellido ?? "").Trim()).Contains(texto)), cedula);
    }

    public async Task<ClienteDbDto?> GetByIdAsync(string idCliente, CancellationToken cancellationToken)
    {
        var id = NormalizarId(idCliente);
        return await _dbContext.Clientes
            .AsNoTracking()
            .Where(x => x.IdCliente == id)
            .Select(AClienteDbDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<ClienteDb, ClienteDbDto>> AClienteDbDto = x => new ClienteDbDto(
        x.IdCliente.Trim(),
        x.Apellido,
        x.Nombre,
        x.Telefono,
        x.Estatus,
        x.Direccion,
        x.Comentario,
        x.CodigoPais,
        x.CodigoProvincia,
        x.CodigoDistrito,
        x.CodigoCorregimiento);

    /// <summary>Cédula panameña en formato 000-0000-00000; otros documentos (pasaporte) se respetan tal cual.</summary>
    private static string NormalizarId(string idCliente) =>
        TryNormalizePanamaId(idCliente, out var normalizada) ? normalizada : idCliente.Trim();

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

    public async Task<ClienteDbDto> CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ClienteDbValidacion.Validar(dto);
        dto = dto with { IdCliente = NormalizarId(dto.IdCliente) };

        var exists = await _dbContext.Clientes
            .AsNoTracking()
            .AnyAsync(x => x.IdCliente == dto.IdCliente, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException($"Ya existe un cliente registrado con la cédula {dto.IdCliente}.");
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

        return (await GetByIdAsync(dto.IdCliente, cancellationToken))!;
    }

    public async Task<ClienteDbDto> UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ClienteDbValidacion.Validar(dto);
        dto = dto with { IdCliente = NormalizarId(dto.IdCliente) };

        var entity = await _dbContext.Clientes
            .FirstOrDefaultAsync(x => x.IdCliente == dto.IdCliente, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException($"No existe un cliente con la cédula {dto.IdCliente}.");
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

        return (await GetByIdAsync(dto.IdCliente, cancellationToken))!;
    }

    public async Task<bool> ActualizarComentarioAsync(string idCliente, string? comentario, CancellationToken cancellationToken)
    {
        var filas = await _dbContext.Clientes
            .Where(x => x.IdCliente == idCliente)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Comentario, comentario), cancellationToken);

        return filas > 0;
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

}
