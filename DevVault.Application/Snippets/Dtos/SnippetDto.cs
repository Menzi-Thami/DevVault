using DevVault.Domain.Entities;

namespace DevVault.Application.Snippets.Dtos;

public sealed record SnippetDto(
    Guid Id,
    string Title,
    string Content,
    string Language,
    DateTime CreatedAt,
    Guid CreatedByUserId)
{
    public static SnippetDto FromEntity(Snippet s) =>
        new(s.Id, s.Title, s.Content, s.Language, s.CreatedAt, s.CreatedByUserId);
}
