using System.Net;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Hosting;

public sealed class HealthCheckTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]   // includes the DbContext check against the real database
    public async Task HealthEndpoint_Returns200_WithoutAToken(string path)
    {
        var response = await AnonymousClient.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}

/// <summary>
/// Hosts built with the production configuration path (no test auth scheme), so these exercise
/// the real startup validation and the real JWT bearer wiring.
/// </summary>
public sealed class StartupTests
{
    [Fact]
    public void Startup_WithoutConnectionString_FailsAtBoot()
    {
        using var factory = CreateFactory(connectionString: "");

        ShouldFailValidation(factory, "ConnectionStrings:DefaultConnection");
    }

    [Fact]
    public void Startup_WithoutJwtAuthority_FailsAtBoot()
    {
        using var factory = CreateFactory(authority: "");

        ShouldFailValidation(factory, nameof(DevVault.API.Authentication.JwtAuthenticationOptions.Authority));
    }

    [Fact]
    public void Startup_WithoutTelemetryServiceName_FailsAtBoot()
    {
        using var factory = CreateFactory().WithWebHostBuilder(builder =>
            builder.UseSetting("Observability:ServiceName", ""));

        ShouldFailValidation(factory, nameof(DevVault.API.Observability.ObservabilityOptions.ServiceName));
    }

    [Fact]
    public async Task AnonymousRequest_IsChallengedForABearerToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/snippets");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldContain(h => h.Scheme == "Bearer");
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string? connectionString = null, string? authority = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString ?? DevVaultApiFactory.ConnectionString);
            builder.UseSetting("Authentication:Jwt:Authority", authority ?? TestSettings.JwtAuthority);
            builder.UseSetting("Authentication:Jwt:Audience", TestSettings.JwtAudience);
        });

    private static void ShouldFailValidation(WebApplicationFactory<Program> factory, string expectedInMessage)
    {
        var thrown = Should.Throw<Exception>(() => factory.CreateClient());

        Flatten(thrown).OfType<OptionsValidationException>()
            .ShouldContain(e => e.Message.Contains(expectedInMessage, StringComparison.Ordinal),
                $"expected an OptionsValidationException mentioning {expectedInMessage}, got {thrown}");
    }

    private static IEnumerable<Exception> Flatten(Exception exception)
    {
        yield return exception;
        var inner = exception is AggregateException aggregate
            ? aggregate.InnerExceptions
            : exception.InnerException is null ? [] : [exception.InnerException];
        foreach (var child in inner.SelectMany(Flatten))
            yield return child;
    }
}
