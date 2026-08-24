using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Domain.Entities.Clientes.Events;

public sealed record ClienteCreatedDomainEvent(Guid ClienteId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}