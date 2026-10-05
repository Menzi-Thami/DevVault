using System.Net;
using System.Net.Http.Json;
using DevVault.Application.Snippets.Dtos;
using DevVault.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Snippets;

public sealed class SnippetsApiTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Guid User = Guid.Parse("8c7a3f52-1b9d-4c0e-9a51-3f2d6e4b7a10");

    [Fact]
    public async Task Create_Returns201_WithLocationThatResolvesToTheSnippet()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets", ValidBody());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<SnippetDto>();
        created.ShouldNotBeNull();
        response.Headers.Location.ShouldNotBeNull();

        var fetched = await Client.GetFromJsonAsync<SnippetDto>(response.Headers.Location);
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(created.Id);
        fetched.Title.ShouldBe("Hello");
        fetched.Language.ShouldBe("C#");   // round-trips through the Language value converter
    }

    [Fact]
    public async Task GetById_Unknown_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync($"/api/snippets/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Create_WithBlankTitle_Returns400ProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets", ValidBody(title: "   "));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Create_WithMissingTitle_Returns400ValidationProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets",
            new { content = "code", language = "C#", createdByUserId = User });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"errors\"");
        body.ShouldContain("Title");
    }

    [Fact]
    public async Task List_ReturnsNewestFirst()
    {
        await Client.PostAsJsonAsync("/api/snippets", ValidBody(title: "older"));
        Factory.Time.Advance(TimeSpan.FromMinutes(1));
        await Client.PostAsJsonAsync("/api/snippets", ValidBody(title: "newer"));

        var list = await Client.GetFromJsonAsync<List<SnippetDto>>("/api/snippets");

        list.ShouldNotBeNull();
        list.Select(s => s.Title).ShouldBe(["newer", "older"]);
    }

    private static object ValidBody(string title = "Hello") =>
        new { title, content = "Console.WriteLine();", language = "C#", createdByUserId = User };
}
