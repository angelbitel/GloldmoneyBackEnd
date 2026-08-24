namespace GoldmoneyBackend.Domain.Common;

public sealed class ConflictDomainException : DomainException
{
    public ConflictDomainException(string message)
        : base(message)
    {
    }
}