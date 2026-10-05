namespace DevVault.Infrastructure;

public static class HealthCheckTags
{
    /// <summary>Checks that must pass before the instance takes traffic (/health/ready).</summary>
    public const string Ready = "ready";
}
