namespace GoldmoneyBackend.Api.Contracts.ClientesDb;

public sealed record UpdateClienteDbRequest(
    string? Apellido,
    string? Nombre,
    string? Telefono,
    int? Estatus,
    string? Direccion,
    string? Comentario,
    string? CodigoPais,
    string? CodigoProvincia,
    string? CodigoDistrito,
    string? CodigoCorregimiento);
