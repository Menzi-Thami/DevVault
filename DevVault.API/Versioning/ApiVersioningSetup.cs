using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DevVault.API.Versioning;

/// <summary>
/// URL-segment versioning (<c>/api/v1/...</c>) with one OpenAPI document per version. A breaking
/// contract change ships as a new version alongside the old one, which is then marked
/// <c>[ApiVersion(..., Deprecated = true)]</c> before it is removed.
/// </summary>
public static class ApiVersioningSetup
{
    public static IServiceCollection AddUrlSegmentApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                // Every route carries its version; there is no unversioned legacy surface to default.
                options.DefaultApiVersion = new ApiVersion(1);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                // api-supported-versions / api-deprecated-versions response headers.
                options.ReportApiVersions = true;
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerDocumentPerVersion>();
        return services;
    }

    /// <summary>A Swagger UI entry for each version's document.</summary>
    public static void AddVersionEndpoints(this SwaggerUIOptions options, IApiVersionDescriptionProvider versions)
    {
        foreach (var description in versions.ApiVersionDescriptions)
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
    }

    private sealed class SwaggerDocumentPerVersion(IApiVersionDescriptionProvider versions)
        : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in versions.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = "DevVault API",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated ? "This version is deprecated." : null
                });
            }
        }
    }
}
