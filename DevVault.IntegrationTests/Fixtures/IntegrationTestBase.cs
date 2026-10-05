using Xunit;

namespace DevVault.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class SharedApiFixture : ICollectionFixture<DevVaultApiFactory>
{
    public const string Name = "api";
}

/// <summary>Every test starts from empty tables; <see cref="Client"/> is signed in as <see cref="UserA"/>.</summary>
[Collection(SharedApiFixture.Name)]
public abstract class IntegrationTestBase(DevVaultApiFactory factory) : IAsyncLifetime
{
    protected static readonly Guid UserA = Guid.Parse("8c7a3f52-1b9d-4c0e-9a51-3f2d6e4b7a10");
    protected static readonly Guid UserB = Guid.Parse("d41e0b77-52c3-4f8a-b6e9-0a9c3d5f2b84");

    protected DevVaultApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClientFor(UserA);

    protected HttpClient AnonymousClient { get; } = factory.CreateClient();

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        AnonymousClient.Dispose();
        return Task.CompletedTask;
    }
}
