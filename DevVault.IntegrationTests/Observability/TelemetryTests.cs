using System.Diagnostics;
using System.Net;
using DevVault.API.Observability;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Observability;

/// <summary>
/// The real OpenTelemetry pipeline with an in-memory exporter added on top, so these check what
/// the instrumentation actually records rather than that some registration call was made.
/// </summary>
public sealed class TelemetryTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Request_IsTraced_WithItsSqlQueryAsAChildSpanInTheSameTrace()
    {
        var spans = new ExportedItems<Activity>();
        await using var traced = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(spans))));
        using var client = traced.CreateClientFor(UserA);

        (await client.GetAsync("/api/v1/snippets")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var request = await spans.WaitForAsync(s => s.Kind == ActivityKind.Server);
        request.GetTagItem("http.route").ShouldBe("api/v{version:apiVersion}/snippets");
        var query = await spans.WaitForAsync(s => s.Kind == ActivityKind.Client
            && s.TagObjects.Any(t => t.Key.StartsWith("db.", StringComparison.Ordinal)));
        query.TraceId.ShouldBe(request.TraceId);
    }

    [Fact]
    public async Task HealthProbes_AreNotTraced()
    {
        var spans = new ExportedItems<Activity>();
        await using var traced = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(spans))));
        using var client = traced.CreateClientFor(UserA);

        await client.GetAsync("/health/live");
        await client.GetAsync("/api/v1/snippets");

        await spans.WaitForAsync(s => s.Kind == ActivityKind.Server);
        spans.Snapshot().ShouldNotContain(s => s.Kind == ActivityKind.Server && s.DisplayName.Contains("health", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Metrics_IncludeHttpServerAndRuntimeInstruments()
    {
        var metrics = new ExportedItems<Metric>();
        await using var metered = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureOpenTelemetryMeterProvider(meters => meters.AddInMemoryExporter(metrics))));
        using var client = metered.CreateClientFor(UserA);

        await client.GetAsync("/api/v1/snippets");
        metered.Services.GetRequiredService<MeterProvider>().ForceFlush().ShouldBeTrue();

        var names = metrics.Snapshot().Select(m => m.Name).ToList();
        names.ShouldContain("http.server.request.duration");
        names.ShouldContain(n => n.StartsWith("dotnet.", StringComparison.Ordinal));   // runtime: GC, thread pool, ...
    }

    [Fact]
    public async Task WithBothExportersConfigured_TheApiStillBootsAndServes()
    {
        // Unreachable endpoints: export failures must never surface as request failures.
        await using var exporting = Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting(ObservabilitySetup.OtlpEndpointKey, "http://127.0.0.1:9");
            b.UseSetting(ObservabilitySetup.AzureMonitorConnectionStringKey,
                "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/");
        });
        using var client = exporting.CreateClientFor(UserA);

        (await client.GetAsync("/api/v1/snippets")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null, null, false, false)]
    [InlineData("http://localhost:4317", null, true, false)]
    [InlineData(null, "InstrumentationKey=00000000-0000-0000-0000-000000000000", false, true)]
    [InlineData("http://localhost:4317", "InstrumentationKey=00000000-0000-0000-0000-000000000000", true, true)]
    [InlineData("  ", "", false, false)]
    public void Exporters_FollowTheStandardVariables(string? otlpEndpoint, string? appInsights, bool otlp, bool azureMonitor)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ObservabilitySetup.OtlpEndpointKey] = otlpEndpoint,
                [ObservabilitySetup.AzureMonitorConnectionStringKey] = appInsights
            })
            .Build();

        TelemetryExporters.From(configuration).ShouldBe(new TelemetryExporters(otlp, azureMonitor));
    }
}
