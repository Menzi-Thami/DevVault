using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;
using DevVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DevVault.Application.Snippets.Commands.CreateSnippet;

/// <summary>
/// Create-snippet use case. Single responsibility (SRP). Depends only on the
/// repository port and an injected <see cref="TimeProvider"/> — no ambient
/// statics, no EF Core — so it is trivially unit-testable.
/// </summary>
public sealed class CreateSnippetHandler(
    ISnippetRepository repository,
    TimeProvider timeProvider,
    ILogger<CreateSnippetHandler> logger)
{
    public async Task<SnippetDto> HandleAsync(
        CreateSnippetCommand command, CancellationToken cancellationToken = default)
    {
        var snippet = Snippet.Create(
            command.Title,
            command.Content,
            command.Language,
            command.CreatedByUserId,
            timeProvider.GetUtcNow().UtcDateTime);

        await repository.AddAsync(snippet, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Notable business event. Log identifiers/metadata only — never the
        // snippet title or content body.
        logger.LogInformation(
            "Created snippet {SnippetId} in {Language} for user {UserId}",
            snippet.Id, snippet.Language.Value, snippet.CreatedByUserId);

        return SnippetDto.FromEntity(snippet);
    }
}
