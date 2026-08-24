namespace GoldmoneyBackend.Application.Clientes.DTOs;

public sealed record ClienteDto(
    Guid Id,
    string Nombre,
    string Email,
    string Documento,
    string Estado,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);