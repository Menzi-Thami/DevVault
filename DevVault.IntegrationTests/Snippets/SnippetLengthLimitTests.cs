using System.Net;
using System.Net.Http.Json;
using DevVault.Domain.Entities;
using DevVault.Domain.ValueObjects;
using DevVault.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Snippets;

/// <summary>
/// Oversize input used to pass the domain and fail in SaveChanges with a SQL truncation error,
/// which surfaced as a 500. It must be rejected as the client's mistake.
/// </summary>
public sealed class SnippetLengthLimitTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Guid User = Guid.Parse("8c7a3f52-1b9d-4c0e-9a51-3f2d6e4b7a10");

    [Fact]
    public async Task Create_WithTitleOverLimit_Returns400()
    {
        var response = await Post(title: new string('t', Snippet.TitleMaxLength + 1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Create_WithLanguageOverLimit_Returns400()
    {
        var response = await Post(language: new string('l', Language.MaxLength + 1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithContentOverLimit_Returns400()
    {
        var response = await Post(content: new string('c', Snippet.ContentMaxLength + 1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AtEveryLimit_IsStored()
    {
        var response = await Post(
            title: new string('t', Snippet.TitleMaxLength),
            content: new string('c', Snippet.ContentMaxLength),
            language: new string('l', Language.MaxLength));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private Task<HttpResponseMessage> Post(string title = "Title", string content = "code", string language = "C#") =>
        Client.PostAsJsonAsync("/api/snippets", new { title, content, language, createdByUserId = User });
}
