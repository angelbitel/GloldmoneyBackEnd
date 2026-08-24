using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Entities.Clientes.Events;
using GoldmoneyBackend.Domain.Enums;
using GoldmoneyBackend.Domain.ValueObjects;

namespace GoldmoneyBackend.Domain.Entities.Clientes;

public sealed class Cliente : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public Documento Documento { get; private set; } = null!;
    public ClienteEstado Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    private Cliente()
    {
    }

    public static Cliente Create(string nombre, Email email, Documento documento)
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = ValidateNombre(nombre),
            Email = email,
            Documento = documento,
            Estado = ClienteEstado.Activo,
            FechaCreacion = DateTime.UtcNow
        };

        cliente.RaiseDomainEvent(new ClienteCreatedDomainEvent(cliente.Id));
        return cliente;
    }

    public void Update(string nombre, Email email, Documento documento)
    {
        Nombre = ValidateNombre(nombre);
        Email = email;
        Documento = documento;
        FechaActualizacion = DateTime.UtcNow;

        RaiseDomainEvent(new ClienteUpdatedDomainEvent(Id));
    }

    public void CambiarEstado(ClienteEstado estado)
    {
        if (!Enum.IsDefined(estado))
        {
            throw new DomainValidationException("El estado del cliente no es valido.");
        }

        if (Estado == estado)
        {
            return;
        }

        Estado = estado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        RaiseDomainEvent(new ClienteDeletedDomainEvent(Id));
    }

    private static string ValidateNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainValidationException("El nombre del cliente es obligatorio.");
        }

        var normalized = nombre.Trim();

        if (normalized.Length is < 2 or > 200)
        {
            throw new DomainValidationException("El nombre del cliente debe tener entre 2 y 200 caracteres.");
        }

        return normalized;
    }
}