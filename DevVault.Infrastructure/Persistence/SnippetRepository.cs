using DevVault.Application.Common.Interfaces;
using DevVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevVault.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="ISnippetRepository"/>. This is the only place
/// that knows about the DbContext; the Application layer stays persistence-agnostic.
/// </summary>
public sealed class SnippetRepository(AppDbContext context) : ISnippetRepository
{
    public async Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default) =>
        await context.Snippets.AddAsync(snippet, cancellationToken);

    public async Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Snippets.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Snippet>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Snippets
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
