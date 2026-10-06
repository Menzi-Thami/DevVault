using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DevVault.API.ErrorHandling;
using DevVault.API.Observability;
using DevVault.API.RateLimiting;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.RateLimiting;

/// <summary>
/// Each test gets its own host (limiter state is per host) with limits small enough to trip.
/// </summary>
public sealed class RateLimitingTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_BeyondItsLimit_Returns429ProblemDetails_WithRetryAfter()
    {
        await using var host = LimitedHost(limits => limits.CreatePermitLimit = 2);
        using var client = host.CreateClientFor(UserA);

        (await client.PostAsJsonAsync("/api/v1/snippets", ValidBody())).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/v1/snippets", ValidBody())).StatusCode.ShouldBe(HttpStatusCode.Created);
        var rejected = await client.PostAsJsonAsync("/api/v1/snippets", ValidBody());

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        rejected.Headers.RetryAfter.Delta.ShouldNotBeNull();
        rejected.Headers.RetryAfter.Delta.Value.ShouldBeInRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));
        rejected.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().ShouldBe(429);
        problem.GetProperty("code").GetString().ShouldBe(ErrorCodes.RateLimited);
        problem.GetProperty("traceId").GetString().ShouldBe(rejected.Headers.GetValues(TraceCorrelation.HeaderName).Single());
    }

    [Fact]
    public async Task CreateLimit_DoesNotLimitReads()
    {
        await using var host = LimitedHost(limits => limits.CreatePermitLimit = 1);
        using var client = host.CreateClientFor(UserA);

        await client.PostAsJsonAsync("/api/v1/snippets", ValidBody());
        (await client.PostAsJsonAsync("/api/v1/snippets", ValidBody())).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        (await client.GetAsync("/api/v1/snippets")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Limits_ArePerUser()
    {
        await using var host = LimitedHost(limits => limits.CreatePermitLimit = 1);
        using var userA = host.CreateClientFor(UserA);
        using var userB = host.CreateClientFor(UserB);

        await userA.PostAsJsonAsync("/api/v1/snippets", ValidBody());
        (await userA.PostAsJsonAsync("/api/v1/snippets", ValidBody())).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        (await userB.PostAsJsonAsync("/api/v1/snippets", ValidBody())).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GlobalBucket_LimitsEveryEndpoint()
    {
        await using var host = LimitedHost(limits =>
        {
            limits.TokenLimit = 3;
            limits.TokensPerPeriod = 1;
            limits.ReplenishmentPeriodSeconds = 3600;
        });
        using var client = host.CreateClientFor(UserA);

        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/api/v1/snippets")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var rejected = await client.GetAsync("/api/v1/snippets");
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter?.Delta.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthProbes_AreNeverRateLimited(string path)
    {
        await using var host = LimitedHost(limits =>
        {
            limits.TokenLimit = 1;
            limits.TokensPerPeriod = 1;
            limits.ReplenishmentPeriodSeconds = 3600;
        });
        using var client = host.CreateClient();

        for (var i = 0; i < 5; i++)
            (await client.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Rejections_AreRecordedAsMetrics()
    {
        var metrics = new ExportedItems<Metric>();
        await using var host = LimitedHost(
            limits => limits.CreatePermitLimit = 1,
            services => services.ConfigureOpenTelemetryMeterProvider(m => m.AddInMemoryExporter(metrics)));
        using var client = host.CreateClientFor(UserA);

        await client.PostAsJsonAsync("/api/v1/snippets", ValidBody());
        await client.PostAsJsonAsync("/api/v1/snippets", ValidBody());
        host.Services.GetRequiredService<MeterProvider>().ForceFlush().ShouldBeTrue();

        metrics.Snapshot().Select(m => m.Name).ShouldContain("aspnetcore.rate_limiting.requests");
    }

    [Fact]
    public void PartitionKey_IsTheUserWhenSignedIn_ElseTheClientIp()
    {
        var anonymous = new DefaultHttpContext();
        anonymous.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        var signedIn = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", UserA.ToString())], "Test"))
        };
        signedIn.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        RateLimitingSetup.PartitionKey(anonymous).ShouldBe("ip:203.0.113.7");
        RateLimitingSetup.PartitionKey(signedIn).ShouldBe($"user:{UserA}");
        RateLimitingSetup.PartitionKey(new DefaultHttpContext()).ShouldBe("ip:unknown");
    }

    private WebApplicationFactory<Program> LimitedHost(
        Action<RateLimitingOptions> limits, Action<IServiceCollection>? services = null) =>
        Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(s =>
        {
            // Registered after the shared factory's generous limits, so these win.
            s.PostConfigure<RateLimitingOptions>(options =>
            {
                options.TokenLimit = 1_000;
                options.TokensPerPeriod = 1_000;
                options.ReplenishmentPeriodSeconds = 60;
                options.CreatePermitLimit = 1_000;
                options.CreateWindowSeconds = 60;
                limits(options);
            });
            services?.Invoke(s);
        }));

    private static object ValidBody() => new { title = "Hello", content = "Console.WriteLine();", language = "C#" };
}
