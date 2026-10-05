using System.Net;
using System.Net.Http.Json;
using DevVault.Application.Common.Paging;
using DevVault.Application.Snippets.Dtos;
using DevVault.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Snippets;

/// <summary>The owner comes from the token, and every read is scoped to it.</summary>
public sealed class SnippetOwnershipTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("GET", "/api/snippets")]
    [InlineData("GET", "/api/snippets/8c7a3f52-1b9d-4c0e-9a51-3f2d6e4b7a10")]
    [InlineData("POST", "/api/snippets")]
    public async Task AnonymousRequest_Returns401(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(new { title = "t", content = "c", language = "C#" });

        var response = await AnonymousClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_StampsTheTokenUser_EvenWhenTheBodyNamesSomeoneElse()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets",
            new { title = "t", content = "c", language = "C#", createdByUserId = UserB });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<SnippetDto>())!.CreatedByUserId.ShouldBe(UserA);
    }

    [Fact]
    public async Task GetById_AnotherUsersSnippet_Returns404()
    {
        var created = await CreateAsUserA();
        using var userB = Factory.CreateClientFor(UserB);

        var response = await userB.GetAsync($"/api/snippets/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);   // not 403: existence isn't confirmed
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallersSnippets()
    {
        var mine = await CreateAsUserA();
        using var userB = Factory.CreateClientFor(UserB);
        await userB.PostAsJsonAsync("/api/snippets", new { title = "theirs", content = "c", language = "C#" });

        var page = await Client.GetFromJsonAsync<CursorPage<SnippetSummaryDto>>("/api/snippets");

        page.ShouldNotBeNull();
        page.Items.Select(s => s.Id).ShouldBe([mine.Id]);
    }

    private async Task<SnippetDto> CreateAsUserA()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets", new { title = "mine", content = "c", language = "C#" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SnippetDto>())!;
    }
}
