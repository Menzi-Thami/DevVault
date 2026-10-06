using System.Net;
using System.Net.Http.Json;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace DevVault.IntegrationTests.Versioning;

public sealed class ApiVersioningTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Responses_ReportTheSupportedVersions()
    {
        var response = await Client.GetAsync("/api/v1/snippets");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("api-supported-versions").Single().ShouldBe("1.0");
    }

    [Fact]
    public async Task Create_LocationPointsAtTheSameVersion()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/snippets",
            new { title = "Hello", content = "Console.WriteLine();", language = "C#" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.AbsolutePath.ShouldStartWith("/api/v1/snippets/");
    }

    [Fact]
    public async Task UnversionedRoute_IsGone()
    {
        var response = await Client.GetAsync("/api/snippets");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnsupportedVersion_Is404ProblemDetails()
    {
        var response = await Client.GetAsync("/api/v2/snippets");

        // Asp.Versioning's answer for a version in the path that no controller serves.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public void OpenApiDocument_ForV1_HasConcreteVersionedPaths()
    {
        var document = Factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        document.Info.Version.ShouldBe("1.0");
        document.Paths.Keys.ShouldBe(["/api/v1/snippets", "/api/v1/snippets/{id}"], ignoreOrder: true);
    }
}
