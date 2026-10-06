using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevVault.API.Observability;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Observability;

/// <summary>A client-visible id that finds the request's logs: header, ProblemDetails and log records agree.</summary>
public sealed class CorrelationTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task EveryResponse_CarriesAW3CTraceIdHeader()
    {
        var ok = await Client.GetAsync("/api/v1/snippets");
        var notFound = await Client.GetAsync($"/api/v1/snippets/{Guid.NewGuid()}");
        var unauthorized = await AnonymousClient.GetAsync("/api/v1/snippets");

        foreach (var response in new[] { ok, notFound, unauthorized })
            TraceIdHeader(response).ShouldMatch("^[0-9a-f]{32}$");
        TraceIdHeader(ok).ShouldNotBe(TraceIdHeader(notFound));
    }

    [Fact]
    public async Task IncomingTraceparent_IsContinued()
    {
        const string callerTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/snippets");
        request.Headers.Add("traceparent", $"00-{callerTraceId}-00f067aa0ba902b7-01");

        var response = await Client.SendAsync(request);

        TraceIdHeader(response).ShouldBe(callerTraceId);
    }

    [Fact]
    public async Task ErrorBody_TraceId_MatchesTheHeader()
    {
        var notFound = await Client.GetAsync($"/api/v1/snippets/{Guid.NewGuid()}");
        var invalid = await Client.PostAsJsonAsync("/api/v1/snippets", new { content = "code", language = "C#" });

        foreach (var response in new[] { notFound, invalid })
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            problem.GetProperty("traceId").GetString().ShouldBe(TraceIdHeader(response));
        }
    }

    [Fact]
    public async Task LogsWrittenDuringARequest_CarryItsTraceId()
    {
        var logs = new CapturingLoggerProvider();
        var otelLogs = new ExportedItems<LogRecord>();
        await using var logged = Factory.WithWebHostBuilder(b =>
        {
            b.ConfigureLogging(logging => logging.AddProvider(logs));
            b.ConfigureTestServices(services =>
                services.ConfigureOpenTelemetryLoggerProvider(provider => provider.AddInMemoryExporter(otelLogs)));
        });
        using var client = logged.CreateClientFor(UserA);

        // GetSnippetByIdHandler logs a warning for an unknown id.
        var response = await client.GetAsync($"/api/v1/snippets/{Guid.NewGuid()}");
        var traceId = TraceIdHeader(response);

        var warning = logs.Entries.Single(e => e.Message.Contains("was not found", StringComparison.Ordinal));
        warning.Scope["TraceId"]?.ToString().ShouldBe(traceId);
        warning.Scope.ShouldContainKey("SpanId");

        var exported = await otelLogs.WaitForAsync(r =>
            r.FormattedMessage?.Contains("was not found", StringComparison.Ordinal) == true);
        exported.TraceId.ToHexString().ShouldBe(traceId);
    }

    private static string TraceIdHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues(TraceCorrelation.HeaderName, out var values)
            ? values.Single()
            : throw new ShouldAssertException($"{response.StatusCode} response has no {TraceCorrelation.HeaderName} header.");
}
