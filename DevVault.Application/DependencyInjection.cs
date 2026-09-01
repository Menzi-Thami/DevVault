using DevVault.Application.Snippets.Commands.CreateSnippet;
using DevVault.Application.Snippets.Queries.GetSnippetById;
using DevVault.Application.Snippets.Queries.ListSnippets;
using Microsoft.Extensions.DependencyInjection;

namespace DevVault.Application;

/// <summary>
/// Composition root for the Application layer. The API calls this so it never
/// needs to know the layer's internal use-case types individually (OCP).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateSnippetHandler>();
        services.AddScoped<GetSnippetByIdHandler>();
        services.AddScoped<ListSnippetsHandler>();
        return services;
    }
}
