using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevVault.API.ErrorHandling;
using DevVault.Application.Common.Interfaces;
using DevVault.Domain.Entities;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.ErrorHandling;

/// <summary>Every error is RFC 9457 ProblemDetails with a traceId; ours also carry a stable code.</summary>
public sealed class ErrorResponseTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task UnknownId_Returns404ProblemDetails_WithCodeAndTraceId()
    {
        var response = await Client.GetAsync($"/api/snippets/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblem(response);
        problem.GetProperty("status").GetInt32().ShouldBe(404);
        problem.GetProperty("type").GetString().ShouldNotBeNullOrWhiteSpace();
        problem.GetProperty("code").GetString().ShouldBe(ErrorCodes.NotFound);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DomainRuleViolation_Returns400ProblemDetails_WithCodeAndTraceId()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets",
            new { title = "   ", content = "code", language = "C#" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblem(response);
        problem.GetProperty("code").GetString().ShouldBe(ErrorCodes.DomainRuleViolated);
        problem.GetProperty("detail").GetString().ShouldBe("Title cannot be empty");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ModelBindingFailure_Returns400ValidationProblemDetails_WithTraceId()
    {
        var response = await Client.PostAsJsonAsync("/api/snippets",
            new { content = "code", language = "C#" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblem(response);
        problem.GetProperty("errors").TryGetProperty("Title", out _).ShouldBeTrue();
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ArgumentException_FromOurOwnCode_Is500_LoggedAsError_AndLeaksNothing()
    {
        var logs = new CapturingLoggerProvider();
        var repository = Substitute.For<ISnippetRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Snippet?>(new ArgumentNullException("secretInternalParameter")));

        await using var faulty = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
            builder.ConfigureTestServices(services => services.AddScoped(_ => repository));
        });
        using var client = faulty.CreateClientFor(UserA);

        var response = await client.GetAsync($"/api/snippets/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("secretInternalParameter");
        (await ReadProblem(response)).GetProperty("code").GetString().ShouldBe(ErrorCodes.Unexpected);
        logs.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Exception is ArgumentNullException);
    }

    private static async Task<JsonElement> ReadProblem(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
