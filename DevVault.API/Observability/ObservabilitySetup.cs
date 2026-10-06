using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevVault.API.Observability;

public static class ObservabilitySetup
{
    /// <summary>Standard OpenTelemetry variable; the OTLP exporter reads the rest (protocol, headers) itself.</summary>
    public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>Standard Application Insights variable, read by <c>UseAzureMonitor()</c>.</summary>
    public const string AzureMonitorConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Traces, metrics and logs through OpenTelemetry. The instrumentation is always on; what is
    /// exported depends only on which standard variables are set (see <see cref="TelemetryExporters"/>),
    /// so a developer machine or test host with neither set exports nothing and needs no collector.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ObservabilityOptions>()
            .BindConfiguration(ObservabilityOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var openTelemetry = services.AddOpenTelemetry()
            // Resolved from the validated options rather than read from raw configuration here.
            .ConfigureResource(resource => resource.AddDetector(sp =>
                new ServiceNameDetector(sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value.ServiceName)))
            .WithTracing(tracing => tracing
                // Probes hit these every few seconds; tracing them only buries real requests.
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
                .AddHttpClientInstrumentation()
                // EF Core talks to SQL Server through Microsoft.Data.SqlClient, so this captures
                // every query EF sends, as a child span of the request that caused it.
                .AddSqlClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging(configureBuilder: null, configureOptions: options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });

        var exporters = TelemetryExporters.From(configuration);
        if (exporters.Otlp)
            openTelemetry.UseOtlpExporter();
        if (exporters.AzureMonitor)
            openTelemetry.UseAzureMonitor();

        return services;
    }

    private sealed class ServiceNameDetector(string serviceName) : IResourceDetector
    {
        // No auto-generated instance id: each signal's provider calls Detect separately, so a
        // random id here would differ between traces, metrics and logs from the same process.
        public Resource Detect() =>
            ResourceBuilder.CreateEmpty()
                .AddService(serviceName, autoGenerateServiceInstanceId: false)
                .Build();
    }
}

/// <summary>Which exporters to register. Both may be on; with neither, telemetry stays in-process.</summary>
public sealed record TelemetryExporters(bool Otlp, bool AzureMonitor)
{
    public static TelemetryExporters From(IConfiguration configuration) => new(
        Otlp: !string.IsNullOrWhiteSpace(configuration[ObservabilitySetup.OtlpEndpointKey]),
        AzureMonitor: !string.IsNullOrWhiteSpace(configuration[ObservabilitySetup.AzureMonitorConnectionStringKey]));
}
