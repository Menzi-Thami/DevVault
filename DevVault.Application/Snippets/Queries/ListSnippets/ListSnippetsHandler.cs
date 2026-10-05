using DevVault.Application.Common.Exceptions;
using DevVault.Application.Common.Interfaces;
using DevVault.Application.Common.Paging;
using DevVault.Application.Snippets.Dtos;
using Microsoft.Extensions.Logging;

namespace DevVault.Application.Snippets.Queries.ListSnippets;

/// <summary>
/// Lists the current user's snippets newest first, one bounded page at a time (keyset paging on
/// CreatedAt, Id — stable even when timestamps tie).
/// </summary>
public sealed class ListSnippetsHandler(
    ISnippetRepository repository,
    ICurrentUser currentUser,
    ILogger<ListSnippetsHandler> logger)
{
    public async Task<CursorPage<SnippetSummaryDto>> HandleAsync(
        ListSnippetsQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize ?? ListSnippetsQuery.DefaultPageSize, 1, ListSnippetsQuery.MaxPageSize);

        SnippetCursor? after = null;
        if (query.Cursor is not null && !SnippetCursor.TryDecode(query.Cursor, out after))
            throw new ValidationException("The cursor is not valid. Use the nextCursor value from a previous page.");

        // One extra row tells us whether another page exists without a COUNT query.
        var rows = await repository.ListAsync(currentUser.UserId, pageSize + 1, after, cancellationToken);
        var items = rows.Take(pageSize).ToList();
        var nextCursor = rows.Count > pageSize
            ? new SnippetCursor(items[^1].CreatedAt, items[^1].Id).Encode()
            : null;

        // Read-path detail — Debug so routine list calls don't add noise.
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Listed {Count} snippets (more: {HasMore})", items.Count, nextCursor is not null);

        return new CursorPage<SnippetSummaryDto>(items, nextCursor);
    }
}
