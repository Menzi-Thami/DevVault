using DevVault.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DevVault.API.Authentication;

public static class AuthenticationSetup
{
    /// <summary>
    /// JWT bearer authentication from <see cref="JwtAuthenticationOptions"/>, with every endpoint
    /// requiring an authenticated user unless it opts out with <c>AllowAnonymous</c>.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtAuthenticationOptions>>((bearer, jwt) =>
            {
                bearer.Authority = jwt.Value.Authority;
                bearer.Audience = jwt.Value.Audience;
                // Keep claim names as issued ("oid", "sub") instead of the legacy SOAP URIs.
                bearer.MapInboundClaims = false;
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (HttpCurrentUser.TryGetUserId(context.Principal) is null)
                            context.Fail("The access token has no GUID 'oid' or 'sub' claim.");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }
}
