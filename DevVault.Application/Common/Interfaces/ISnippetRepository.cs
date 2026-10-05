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
    Task<IReadOnlyList<Snippet>> ListAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
