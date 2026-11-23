namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>Clientes de la tabla legacy CLIENTES (clave: id_cliente / cédula).</summary>
public interface IClientesRepository
{
    Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(string? search, CancellationToken cancellationToken);

    /// <summary>Listado paginado con búsqueda, filtro, contratos activos por cliente y totales.</summary>
    Task<ClientesPaginadoDto> GetPaginadoAsync(string? buscar, FiltroClientes filtro, int pagina, int tamanoPagina, CancellationToken cancellationToken);
    Task<ClienteDbDto?> GetByIdAsync(string idCliente, CancellationToken cancellationToken);
    Task<ClienteDbDto> CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
    Task<ClienteDbDto> UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Cambia solo CLIENTES.comentario (null lo borra); el VB6 lo muestra según
    /// PARAMETROS_EMPRESA.mostrar_comentario_cliente. Devuelve false si el cliente no existe.
    /// </summary>
    Task<bool> ActualizarComentarioAsync(string idCliente, string? comentario, CancellationToken cancellationToken);
}
