using DevVault.Application.Common.Interfaces;
using DevVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DevVault.Infrastructure;

/// <summary>Composition root for the Infrastructure layer.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Validated at startup: a missing connection string stops the app at boot instead of
        // letting it report healthy and 500 on the first request.
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.Section)
            .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultConnection),
                $"{DatabaseOptions.Section}:{nameof(DatabaseOptions.DefaultConnection)} is required")
            .ValidateOnStart();

        // EnableRetryOnFailure is incompatible with user-initiated transactions; wrap any future
        // BeginTransaction in Database.CreateExecutionStrategy().ExecuteAsync(...).
        services.AddDbContext<AppDbContext>((sp, options) => options.UseSqlServer(
            sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.DefaultConnection,
            sql => sql.EnableRetryOnFailure()));

        services.AddScoped<ISnippetRepository, SnippetRepository>();

        // Single production clock. Tests substitute FakeTimeProvider instead.
        services.AddSingleton(TimeProvider.System);

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(tags: [HealthCheckTags.Ready]);

        return services;
    }
}
