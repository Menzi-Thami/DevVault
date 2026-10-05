namespace DevVault.Infrastructure.Persistence;

/// <summary>Bound from <c>ConnectionStrings</c> (user-secrets locally, app settings when hosted).</summary>
public sealed class DatabaseOptions
{
    public const string Section = "ConnectionStrings";

    // Nullable rather than "" so EF design-time tooling (migrations add) still works without one;
    // ValidateOnStart rejects it when the app actually runs.
    public string? DefaultConnection { get; set; }
}
