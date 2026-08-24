using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Domain.Entities.Clientes.Events;

public sealed record ClienteDeletedDomainEvent(Guid ClienteId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}