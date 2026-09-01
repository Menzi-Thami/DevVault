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
    public string Language { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // Required by EF Core for materialisation. Not for application use.
    private Snippet() { }

    /// <summary>
    /// Creates a valid snippet. The timestamp is passed in (rather than read
    /// from <c>DateTime.UtcNow</c>) so creation is deterministic and testable;
    /// the caller supplies it from an injected <see cref="TimeProvider"/>.
    /// </summary>
    public static Snippet Create(
        string title,
        string content,
        string language,
        Guid createdByUserId,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be empty", nameof(language));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));

        return new Snippet
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Content = content,
            Language = language.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt
        };
    }
}
