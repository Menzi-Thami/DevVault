using DevVault.Domain.Common;
using DevVault.Domain.ValueObjects;

namespace DevVault.Domain.Entities;

/// <summary>
/// A stored code snippet. Aggregate root: can only be created through
/// <see cref="Create"/>, which enforces its invariants, so an invalid
/// Snippet cannot exist. Setters are private to preserve encapsulation.
/// </summary>
public class Snippet
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public Language Language { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // Required by EF Core for materialisation. Not for application use.
    private Snippet() { }

    /// <summary>
    /// Creates a valid snippet. The timestamp is passed in (rather than read
    /// from <c>DateTime.UtcNow</c>) so creation is deterministic and testable;
    /// the caller supplies it from an injected <see cref="TimeProvider"/>.
    /// Invariant violations throw <see cref="DomainException"/>.
    /// </summary>
    public static Snippet Create(
        string title,
        string content,
        string language,
        Guid createdByUserId,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty");
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Content cannot be empty");
        if (createdByUserId == Guid.Empty)
            throw new DomainException("CreatedByUserId cannot be empty");

        return new Snippet
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Content = content,
            Language = Language.From(language),   // value object self-validates
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt
        };
    }
}
