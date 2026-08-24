namespace GoldmoneyBackend.Domain.Common;

public sealed class NotFoundDomainException : DomainException
{
    public NotFoundDomainException(string message)
        : base(message)
    {
    }
}