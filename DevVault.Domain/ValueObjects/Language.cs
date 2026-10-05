using DevVault.Domain.Common;

namespace DevVault.Domain.ValueObjects;

/// <summary>
/// The programming language of a snippet, modelled as a value object rather
/// than a bare string: it is self-validating, normalised, and compared by
/// value. Making this implicit concept explicit is the DDD point.
/// </summary>
public sealed record Language
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Language(string value) => Value = value;

    public static Language From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Language cannot be empty");

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            throw new DomainException($"Language cannot exceed {MaxLength} characters");

        if (trimmed.Any(char.IsControl))
            throw new DomainException("Language cannot contain control characters");

        return new Language(trimmed);
    }

    public override string ToString() => Value;
}
