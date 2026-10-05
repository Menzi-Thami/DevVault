using DevVault.Application.Common.Exceptions;
using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;
using DevVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DevVault.Application.Snippets.Queries.GetSnippetById;

/// <summary>Get-one use case. Throws <see cref="NotFoundException"/> rather
/// than returning null — the API edge maps that to 404 (no silent defaults).</summary>
public sealed class GetSnippetByIdHandler(
    ISnippetRepository repository,
    ICurrentUser currentUser,
    ILogger<GetSnippetByIdHandler> logger)
{
    public async Task<SnippetDto> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Another user's snippet is reported exactly like a missing one (404, not 403).
        var snippet = await repository.GetByIdAsync(id, currentUser.UserId, cancellationToken);
        if (snippet is null)
        {
            // Recoverable/expected miss — Warning, not Error. The API's exception
            // handler maps the resulting NotFoundException to a 404 without logging it.
            logger.LogWarning("Snippet {SnippetId} was not found", id);
            throw new NotFoundException(nameof(Snippet), id);
        }

        return SnippetDto.FromEntity(snippet);
    }
}
