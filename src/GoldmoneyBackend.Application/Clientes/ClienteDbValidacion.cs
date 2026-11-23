using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Clientes;

/// <summary>Validación de un cliente de la tabla legacy CLIENTES antes de crearlo o actualizarlo.</summary>
public static class ClienteDbValidacion
{
    private static readonly (string Campo, Func<ClienteDbUpsertDto, string?> Valor, int Max)[] Longitudes =
    [
        ("La cédula", x => x.IdCliente, 14),
        ("El nombre", x => x.Nombre, 20),
        ("El apellido", x => x.Apellido, 20),
        ("El teléfono", x => x.Telefono, 20),
        ("La dirección", x => x.Direccion, 40),
        ("El comentario", x => x.Comentario, 300),
        ("El código de país", x => x.CodigoPais, 5),
        ("El código de provincia", x => x.CodigoProvincia, 5),
        ("El código de distrito", x => x.CodigoDistrito, 5),
        ("El código de corregimiento", x => x.CodigoCorregimiento, 5),
    ];

    public static void Validar(ClienteDbUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdCliente))
            throw new DomainValidationException("La cédula del cliente es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new DomainValidationException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            throw new DomainValidationException("El apellido del cliente es obligatorio.");
        if (dto.Estatus is not null and not (0 or 1))
            throw new DomainValidationException("El estatus del cliente debe ser 0 (inactivo) o 1 (activo).");

        foreach (var (campo, valor, max) in Longitudes)
        {
            var texto = valor(dto)?.Trim();
            if (texto is not null && texto.Length > max)
                throw new DomainValidationException($"{campo} no puede superar los {max} caracteres.");
        }
    }
}
