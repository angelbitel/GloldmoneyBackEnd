using GoldmoneyBackend.Domain.Entities.Clientes;

namespace GoldmoneyBackend.Application.Clientes.DTOs;

public static class ClienteMappings
{
    public static ClienteDto ToDto(this Cliente cliente)
    {
        return new ClienteDto(
            cliente.Id,
            cliente.Nombre,
            cliente.Email.Value,
            cliente.Documento.Value,
            cliente.Estado.ToString(),
            cliente.FechaCreacion,
            cliente.FechaActualizacion);
    }
}