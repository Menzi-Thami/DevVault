namespace DevVault.Application.Snippets.Commands.CreateSnippet;

/// <summary>Input to create a snippet. A plain immutable use-case request.</summary>
public sealed record CreateSnippetCommand(
    string Title,
    string Content,
    string Language,
    Guid CreatedByUserId);
