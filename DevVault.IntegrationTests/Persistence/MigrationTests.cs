using DevVault.Infrastructure.Persistence;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Persistence;

public sealed class MigrationTests(DevVaultApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Migrations_AreAllApplied_AndMatchTheModel()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        // Fails when an entity/configuration change was made without adding a migration.
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
