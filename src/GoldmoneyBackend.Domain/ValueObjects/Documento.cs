using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Domain.ValueObjects;

public sealed record Documento
{
    public string Value { get; }

    private Documento(string value)
    {
        Value = value;
    }

    public static Documento Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El documento es obligatorio.");
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < 5 || normalized.Length > 20)
        {
            throw new DomainValidationException("El documento debe tener entre 5 y 20 caracteres.");
        }

        return new Documento(normalized);
    }

    public override string ToString() => Value;
}