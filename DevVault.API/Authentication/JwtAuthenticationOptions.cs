using System.ComponentModel.DataAnnotations;

namespace DevVault.API.Authentication;

/// <summary>
/// Who issues the access tokens this API accepts. Bound from <c>Authentication:Jwt</c> and
/// validated at startup, so a missing issuer stops the app instead of rejecting every call.
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string Section = "Authentication:Jwt";

    /// <summary>The token issuer, e.g. <c>https://login.microsoftonline.com/{tenant-id}/v2.0</c>.</summary>
    [Required, Url]
    public string Authority { get; set; } = string.Empty;

    /// <summary>The API's app ID URI or client id, e.g. <c>api://{client-id}</c>.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;
}
