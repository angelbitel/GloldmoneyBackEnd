namespace GoldmoneyBackend.Api.Contracts.ClientesDb;

/// <param name="Comentario">Nota del cliente; vacía o null para borrarla.</param>
public sealed record ActualizarComentarioClienteRequest(string? Comentario);
