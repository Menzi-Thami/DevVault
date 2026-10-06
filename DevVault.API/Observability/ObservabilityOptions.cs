using System.ComponentModel.DataAnnotations;

namespace DevVault.API.Observability;

/// <summary>
/// Telemetry identity. Bound from <c>Observability</c> and validated at startup. Where telemetry
/// goes is not configured here: see <see cref="ObservabilitySetup"/> for the exporter switches.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string Section = "Observability";

    /// <summary>The OpenTelemetry <c>service.name</c> (the Application Insights cloud role name).</summary>
    [Required]
    public string ServiceName { get; set; } = string.Empty;
}
