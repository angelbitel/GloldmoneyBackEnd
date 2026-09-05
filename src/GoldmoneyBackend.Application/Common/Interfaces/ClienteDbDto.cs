namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record ClienteDbDto(
    string IdCliente,
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
