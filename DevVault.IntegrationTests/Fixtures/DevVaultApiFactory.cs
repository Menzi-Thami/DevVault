using DevVault.API.RateLimiting;
using DevVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace DevVault.IntegrationTests.Fixtures;

/// <summary>
/// Boots the real API over a real SQL Server database. Locally that is LocalDB; CI points
/// <see cref="ConnectionStringVariable"/> at a SQL Server container, so the same tests run in both.
/// The database is dropped and rebuilt from the migrations once per run (which also tests the
/// migrations), then Respawn empties the tables between tests.
/// </summary>
public sealed class DevVaultApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ConnectionStringVariable = "DEVVAULT_TEST_SQL";

    private const string LocalDbConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=DevVault_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True";

    private Respawner? _respawner;

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable(ConnectionStringVariable) is { Length: > 0 } fromEnv
            ? fromEnv
            : LocalDbConnectionString;

    /// <summary>Starts at a fixed instant; tests move it when they need distinct timestamps.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": that would load the developer's user-secrets into the test host.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        // Must be present to pass startup validation; never contacted, because the test
        // scheme below replaces JWT bearer as the default.
        builder.UseSetting("Authentication:Jwt:Authority", TestSettings.JwtAuthority);
        builder.UseSetting("Authentication:Jwt:Audience", TestSettings.JwtAudience);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);

            // Every test shares this host and mostly the same user, so the production limits would
            // trip across unrelated tests. RateLimitingTests set small limits on their own host.
            services.PostConfigure<RateLimitingOptions>(limits =>
            {
                limits.TokenLimit = 1_000_000;
                limits.TokensPerPeriod = 1_000_000;
                limits.CreatePermitLimit = 1_000_000;
            });

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();
}
