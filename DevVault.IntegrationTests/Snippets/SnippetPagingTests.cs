using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevVault.API.ErrorHandling;
using DevVault.Application.Common.Paging;
using DevVault.Application.Snippets.Dtos;
using DevVault.Application.Snippets.Queries.ListSnippets;
using DevVault.Domain.Entities;
using DevVault.Infrastructure.Persistence;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Snippets;

/// <summary>
/// The list is bounded, projected and keyset-paged. Timestamps are seeded in groups of three so
/// ties straddle page boundaries: paging by CreatedAt alone would skip or repeat rows there.
/// </summary>
public sealed class SnippetPagingTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    private const int SeededForUserA = 150;
    private static readonly DateTimeOffset Newest = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(7)]
    [InlineData(100)]
    public async Task WalkingTheCursor_ReturnsEveryRowOnce_NewestFirst(int pageSize)
    {
        var seeded = await SeedAsync();

        var pages = await WalkAsync(Client, pageSize);

        pages.ShouldAllBe(p => p.Items.Count <= pageSize);
        var walked = pages.SelectMany(p => p.Items).ToList();
        walked.Select(s => s.Id).ShouldBe(seeded, ignoreOrder: true);   // all 150, none twice, none of user B's
        walked.Select(s => s.CreatedAt).ShouldBeInOrder(SortDirection.Descending);
        pages[^1].NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task PageSize_IsCappedOnTheServer()
    {
        await SeedAsync();

        var page = await Client.GetFromJsonAsync<CursorPage<SnippetSummaryDto>>("/api/snippets?pageSize=1000");

        page!.Items.Count.ShouldBe(ListSnippetsQuery.MaxPageSize);
        page.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task Items_DoNotCarryTheContent()
    {
        await SeedAsync();

        var json = await Client.GetFromJsonAsync<JsonElement>("/api/snippets?pageSize=1");

        var item = json.GetProperty("items")[0];
        item.TryGetProperty("content", out _).ShouldBeFalse();
        item.GetProperty("title").GetString().ShouldNotBeNullOrEmpty();
        json.TryGetProperty("nextCursor", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task AMalformedCursor_Returns400()
    {
        var response = await Client.GetAsync("/api/snippets?cursor=definitely-not-a-cursor");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().ShouldBe(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task TheGeneratedSql_IsAProjectedKeysetQuery()
    {
        await SeedAsync();
        var logs = new CapturingLoggerProvider();
        await using var logged = Factory.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(logs)));
        using var client = logged.CreateClientFor(UserA);
        var first = await client.GetFromJsonAsync<CursorPage<SnippetSummaryDto>>("/api/snippets?pageSize=5");

        await client.GetAsync($"/api/snippets?pageSize=5&cursor={first!.NextCursor}");

        var sql = logs.Entries
            .Where(e => e.Category == "Microsoft.EntityFrameworkCore.Database.Command")
            .Select(e => e.Message)
            .Last(m => m.Contains("FROM [Snippets]", StringComparison.Ordinal));
        sql.ShouldNotContain("[Content]");
        sql.ShouldContain("ORDER BY [s].[CreatedAt] DESC, [s].[Id] DESC");
        sql.ShouldContain("[s].[CreatedAt] < @");
        sql.ShouldContain("[s].[Id] < @");
        sql.ShouldContain("TOP(@");
    }

    private async Task<List<Guid>> SeedAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var mine = Enumerable.Range(0, SeededForUserA)
            .Select(i => Snippet.Create($"snippet {i}", "body", "C#", UserA, Newest.AddMinutes(-(i / 3))))
            .ToList();
        var theirs = Enumerable.Range(0, 5)
            .Select(i => Snippet.Create($"other {i}", "body", "C#", UserB, Newest.AddMinutes(-i)));

        db.Snippets.AddRange(mine);
        db.Snippets.AddRange(theirs);
        await db.SaveChangesAsync();
        return mine.ConvertAll(s => s.Id);
    }

    private static async Task<List<CursorPage<SnippetSummaryDto>>> WalkAsync(HttpClient client, int pageSize)
    {
        var pages = new List<CursorPage<SnippetSummaryDto>>();
        string? cursor = null;
        do
        {
            var url = $"/api/snippets?pageSize={pageSize}" + (cursor is null ? "" : $"&cursor={cursor}");
            var page = await client.GetFromJsonAsync<CursorPage<SnippetSummaryDto>>(url);
            pages.Add(page!);
            cursor = page!.NextCursor;
            pages.Count.ShouldBeLessThan(200, "the cursor walk is not terminating");
        } while (cursor is not null);

        return pages;
    }
}
