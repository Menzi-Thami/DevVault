using System.ComponentModel.DataAnnotations;

namespace DevVault.API.RateLimiting;

/// <summary>
/// Per-caller request limits. Bound from <c>RateLimiting</c> and validated at startup, so a
/// missing or zero limit stops the app instead of rejecting (or never limiting) every request.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Token bucket for every endpoint: the burst a caller may send at once.</summary>
    [Range(1, 1_000_000)]
    public int TokenLimit { get; set; }

    /// <summary>Tokens added back to each caller's bucket every <see cref="ReplenishmentPeriodSeconds"/>.</summary>
    [Range(1, 1_000_000)]
    public int TokensPerPeriod { get; set; }

    [Range(1, 3600)]
    public int ReplenishmentPeriodSeconds { get; set; }

    /// <summary>Stricter fixed window for creating snippets, on top of the bucket above.</summary>
    [Range(1, 1_000_000)]
    public int CreatePermitLimit { get; set; }

    [Range(1, 3600)]
    public int CreateWindowSeconds { get; set; }
}
