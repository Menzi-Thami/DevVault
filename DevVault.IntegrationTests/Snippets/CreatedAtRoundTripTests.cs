using System.Net.Http.Json;
using System.Text.Json;
using DevVault.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Snippets;

/// <summary>
/// CreatedAt used to come back from POST with "Z" and from GET (read back from datetime2) with no
/// offset at all, which clients parse as local time. The same instant must read the same everywhere.
/// </summary>
public sealed class CreatedAtRoundTripTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task CreatedAt_IsIdenticalOnPostAndGet_AndCarriesAnOffset()
    {
        var post = await Client.PostAsJsonAsync("/api/snippets",
            new { title = "t", content = "c", language = "C#" });
        var posted = CreatedAt(await post.Content.ReadFromJsonAsync<JsonElement>());

        var fetched = CreatedAt(await Client.GetFromJsonAsync<JsonElement>(post.Headers.Location));

        fetched.ShouldBe(posted);
        DateTimeOffset.Parse(fetched, System.Globalization.CultureInfo.InvariantCulture)
            .ShouldBe(Factory.Time.GetUtcNow());
        (fetched.EndsWith('Z') || fetched.EndsWith("+00:00", StringComparison.Ordinal))
            .ShouldBeTrue($"'{fetched}' has no UTC offset");
    }

    private static string CreatedAt(JsonElement snippet) =>
        snippet.GetProperty("createdAt").GetString()!;
}
