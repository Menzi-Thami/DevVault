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
    public async Task HealthEndpoint_Returns200(string path)
    {
        var response = await Client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}

public sealed class StartupTests
{
    [Fact]
    public void Startup_WithoutConnectionString_FailsAtBoot()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "");
        });

        var thrown = Should.Throw<Exception>(() => factory.CreateClient());

        var validation = Flatten(thrown).OfType<OptionsValidationException>().FirstOrDefault();
        validation.ShouldNotBeNull($"expected an OptionsValidationException, got {thrown}");
        validation.Message.ShouldContain("ConnectionStrings:DefaultConnection");
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
