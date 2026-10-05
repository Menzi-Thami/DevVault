namespace DevVault.Application.Snippets.Dtos;

/// <summary>A list row. No <c>Content</c>: bodies can be large, and the detail endpoint returns them.</summary>
public sealed record SnippetSummaryDto(
    Guid Id,
    string Title,
    string Language,
    DateTimeOffset CreatedAt);
