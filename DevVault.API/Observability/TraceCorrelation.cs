using System.Diagnostics;

namespace DevVault.API.Observability;

/// <summary>
/// One request id everywhere: the W3C trace id of the request's activity is what every log record
/// carries (OpenTelemetry and the logging scope), what Application Insights calls the operation id,
/// what ProblemDetails reports as <c>traceId</c>, and what <see cref="HeaderName"/> returns. A
/// client quoting any of them finds the same logs and spans.
/// </summary>
public static class TraceCorrelation
{
    public const string HeaderName = "X-Trace-Id";

    /// <summary>
    /// The 32-hex-digit trace id, continuing the caller's <c>traceparent</c> when it sent one.
    /// Falls back to the connection-scoped request id only if no W3C activity exists.
    /// </summary>
    public static string CurrentTraceId(HttpContext context) =>
        Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity
            ? activity.TraceId.ToHexString()
            : context.TraceIdentifier;

    /// <summary>
    /// Adds <see cref="HeaderName"/> to every response. Register first: the exception handler
    /// clears headers when it rewrites a failed response, but OnStarting callbacks survive that.
    /// </summary>
    public static IApplicationBuilder UseTraceIdHeader(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            var traceId = CurrentTraceId(context);
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = traceId;
                return Task.CompletedTask;
            });
            return next(context);
        });
}
