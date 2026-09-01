using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;

namespace DevVault.Application.Snippets.Queries.ListSnippets;

/// <summary>List-all use case.</summary>
public sealed class ListSnippetsHandler(ISnippetRepository repository)
{
    public async Task<IReadOnlyList<SnippetDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await repository.ListAsync(cancellationToken);
        return snippets.Select(SnippetDto.FromEntity).ToList();
    }
}
