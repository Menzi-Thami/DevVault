using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;
using Microsoft.Extensions.Logging;

namespace DevVault.Application.Snippets.Queries.ListSnippets;

/// <summary>List-all use case.</summary>
public sealed class ListSnippetsHandler(
    ISnippetRepository repository,
    ILogger<ListSnippetsHandler> logger)
{
    public async Task<IReadOnlyList<SnippetDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await repository.ListAsync(cancellationToken);
        var dtos = snippets.Select(SnippetDto.FromEntity).ToList();

        // Read-path detail — Debug so routine list calls don't add noise.
        logger.LogDebug("Listed {Count} snippets", dtos.Count);

        return dtos;
    }
}
