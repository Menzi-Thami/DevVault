using DevVault.Application.Common.Exceptions;
using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;
using DevVault.Domain.Entities;

namespace DevVault.Application.Snippets.Queries.GetSnippetById;

/// <summary>Get-one use case. Throws <see cref="NotFoundException"/> rather
/// than returning null — the API edge maps that to 404 (no silent defaults).</summary>
public sealed class GetSnippetByIdHandler(ISnippetRepository repository)
{
    public async Task<SnippetDto> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var snippet = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Snippet), id);

        return SnippetDto.FromEntity(snippet);
    }
}
