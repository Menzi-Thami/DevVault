namespace DevVault.Application.Common.Logging;

/// <summary>
/// Plain-text log sinks (the console formatter) write property values into the message, so a
/// caller-supplied value containing a line break could forge an extra log line. Strip them from
/// anything that originated in the request before logging it.
/// </summary>
public static class LogValue
{
    public static string Safe(string? value) =>
        value is null
            ? string.Empty
            : value.Replace("\r", string.Empty, StringComparison.Ordinal)
                   .Replace("\n", string.Empty, StringComparison.Ordinal);
}
