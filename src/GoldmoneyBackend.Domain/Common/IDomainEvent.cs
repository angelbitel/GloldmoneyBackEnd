namespace GoldmoneyBackend.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}