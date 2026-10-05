using Xunit;

namespace DevVault.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class SharedApiFixture : ICollectionFixture<DevVaultApiFactory>
{
    public const string Name = "api";
}

/// <summary>Every test starts from empty tables.</summary>
[Collection(SharedApiFixture.Name)]
public abstract class IntegrationTestBase(DevVaultApiFactory factory) : IAsyncLifetime
{
    protected DevVaultApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
