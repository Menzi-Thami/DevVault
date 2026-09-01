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
    Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Snippet>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
