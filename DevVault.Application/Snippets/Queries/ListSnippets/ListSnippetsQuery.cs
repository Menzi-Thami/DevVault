namespace DevVault.Application.Snippets.Queries.ListSnippets;

/// <summary>
/// <paramref name="PageSize"/> is clamped to 1..<see cref="MaxPageSize"/> (default
/// <see cref="DefaultPageSize"/>); <paramref name="Cursor"/> is the previous page's NextCursor.
/// </summary>
public sealed record ListSnippetsQuery(int? PageSize = null, string? Cursor = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
