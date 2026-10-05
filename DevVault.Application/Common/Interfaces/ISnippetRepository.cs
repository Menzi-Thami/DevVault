using DevVault.Application.Snippets.Dtos;
using DevVault.Application.Snippets.Queries.ListSnippets;
using DevVault.Domain.Entities;

namespace DevVault.Application.Common.Interfaces;

/// <summary>
/// Persistence port for <see cref="Snippet"/>. Defined in Application and
/// implemented in Infrastructure, so the dependency points inward (DIP):
/// use cases depend on this abstraction, never on EF Core.
/// </summary>
public interface ISnippetRepository
{
    Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default);
    // Reads are always scoped to an owner: another user's snippet is indistinguishable from a
    // missing one, so a lookup by id can't confirm it exists (IDOR).
    Task<Snippet?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Up to <paramref name="take"/> of the owner's snippets ordered by CreatedAt DESC, Id DESC,
    /// starting after <paramref name="after"/>; projected, so bodies are never loaded.
    /// </summary>
    Task<IReadOnlyList<SnippetSummaryDto>> ListAsync(
        Guid ownerId, int take, SnippetCursor? after, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
