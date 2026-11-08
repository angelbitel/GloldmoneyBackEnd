namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IClientesRepository
{
    Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(string? search, CancellationToken cancellationToken);
    Task CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Cambia solo CLIENTES.comentario (null lo borra); el VB6 lo muestra según
    /// PARAMETROS_EMPRESA.mostrar_comentario_cliente. Devuelve false si el cliente no existe.
    /// </summary>
    Task<bool> ActualizarComentarioAsync(string idCliente, string? comentario, CancellationToken cancellationToken);
}
