namespace DevVault.API.Contracts;

/// <summary>
/// The POST body. It has no owner field on purpose: the owner is the caller identified by the
/// access token, so a request cannot create snippets as somebody else.
/// </summary>
public sealed record CreateSnippetRequest(string Title, string Content, string Language);
