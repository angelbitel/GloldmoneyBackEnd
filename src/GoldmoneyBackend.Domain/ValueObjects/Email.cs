using System.Net.Mail;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El email es obligatorio.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        try
        {
            _ = new MailAddress(normalized);
        }
        catch
        {
            throw new DomainValidationException("El email no tiene un formato valido.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;
}