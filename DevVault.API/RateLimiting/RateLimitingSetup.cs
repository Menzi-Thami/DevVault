using System.Globalization;
using System.Threading.RateLimiting;
using DevVault.API.Authentication;
using DevVault.API.ErrorHandling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DevVault.API.RateLimiting;

/// <summary>
/// The built-in limiter, partitioned per caller. It runs after authentication/authorization so
/// the caller is known. Limits are per instance (scaled out, they multiply by the instance count)
/// and are fair-use protection, not DDoS protection — that belongs at the edge (Front Door/WAF).
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>Endpoint policy for creating snippets (<c>[EnableRateLimiting(CreatePolicy)]</c>).</summary>
    public const string CreatePolicy = "create";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>((limiter, configured) =>
            {
                var limits = configured.Value;
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.OnRejected = WriteRejectionAsync;

                // Every request, per caller. Endpoints opt out with DisableRateLimiting().
                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetTokenBucketLimiter(PartitionKey(context), _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = limits.TokenLimit,
                        TokensPerPeriod = limits.TokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(limits.ReplenishmentPeriodSeconds),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));

                limiter.AddPolicy(CreatePolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.CreatePermitLimit,
                        Window = TimeSpan.FromSeconds(limits.CreateWindowSeconds),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));
            });

        return services;
    }

    /// <summary>
    /// The signed-in user (the same "oid"/"sub" claim <see cref="HttpCurrentUser"/> uses), else the
    /// client IP. Behind a proxy the IP is the proxy's unless forwarded headers are configured.
    /// </summary>
    public static string PartitionKey(HttpContext context) =>
        HttpCurrentUser.TryGetUserId(context.User) is { } userId
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "The request rate limit was exceeded. Retry after the number of seconds in the Retry-After header.",
                Extensions = { ["code"] = ErrorCodes.RateLimited }
            }
        });
    }
}
