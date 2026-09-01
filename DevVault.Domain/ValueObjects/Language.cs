using DevVault.Domain.Common;

namespace DevVault.Domain.ValueObjects;

/// <summary>
/// The programming language of a snippet, modelled as a value object rather
/// than a bare string: it is self-validating, normalised, and compared by
/// value. Making this implicit concept explicit is the DDD point.
/// </summary>
public sealed record Language
{
    public string Value { get; }

    private Language(string value) => Value = value;

    public static Language From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Language cannot be empty");

        return new Language(value.Trim());
    }

    public override string ToString() => Value;
}
