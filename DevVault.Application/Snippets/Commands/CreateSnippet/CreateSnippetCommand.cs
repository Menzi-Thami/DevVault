namespace DevVault.Application.Snippets.Commands.CreateSnippet;

/// <summary>
/// Input to create a snippet. There is deliberately no owner here: the handler takes it from
/// <see cref="Common.Interfaces.ICurrentUser"/>.
/// </summary>
public sealed record CreateSnippetCommand(
    string Title,
    string Content,
    string Language);
