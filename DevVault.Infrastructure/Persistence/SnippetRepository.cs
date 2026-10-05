using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;
using DevVault.Application.Snippets.Queries.ListSnippets;
using DevVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevVault.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="ISnippetRepository"/>. This is the only place
/// that knows about the DbContext; the Application layer stays persistence-agnostic.
/// </summary>
public sealed class SnippetRepository(AppDbContext context) : ISnippetRepository
{
    // Snippet.Create assigns the id, so no async value generator is involved: plain Add.
    public Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        context.Snippets.Add(snippet);
        return Task.CompletedTask;
    }

    // Only the read-side query handler calls this, so nothing needs tracking.
    public async Task<Snippet?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default) =>
        await context.Snippets.AsNoTracking().FirstOrDefaultAsync(
            s => s.Id == id && s.CreatedByUserId == ownerId, cancellationToken);

    public async Task<IReadOnlyList<SnippetSummaryDto>> ListAsync(
        Guid ownerId, int take, SnippetCursor? after, CancellationToken cancellationToken = default)
    {
        var query = context.Snippets.Where(s => s.CreatedByUserId == ownerId);

        // Keyset: rows strictly after the cursor in (CreatedAt DESC, Id DESC) order. Both the
        // comparison and the ORDER BY run in SQL Server, so they agree on uniqueidentifier ordering.
        if (after is not null)
        {
            query = query.Where(s => s.CreatedAt < after.CreatedAt
                || (s.CreatedAt == after.CreatedAt && s.Id.CompareTo(after.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Take(take)
            .Select(s => new { s.Id, s.Title, s.Language, s.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(r => new SnippetSummaryDto(r.Id, r.Title, r.Language.Value, r.CreatedAt));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
