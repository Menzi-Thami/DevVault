using System.Security.Claims;
using DevVault.Application.Common.Interfaces;

namespace DevVault.API.Authentication;

/// <summary>
/// <see cref="ICurrentUser"/> for an HTTP request. The only class that touches
/// <see cref="IHttpContextAccessor"/>.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // Entra's "oid" is the stable object id; "sub" is accepted from issuers that put a GUID there.
    private static readonly string[] UserIdClaimTypes = ["oid", "sub"];

    public Guid UserId =>
        TryGetUserId(httpContextAccessor.HttpContext?.User)
        ?? throw new InvalidOperationException(
            "ICurrentUser was used without an authenticated user carrying a GUID 'oid' or 'sub' claim.");

    /// <summary>
    /// Also used when a bearer token is validated, so a token without a usable user id is
    /// rejected with 401 before it reaches a handler.
    /// </summary>
    public static Guid? TryGetUserId(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        foreach (var type in UserIdClaimTypes)
        {
            if (Guid.TryParse(user.FindFirst(type)?.Value, out var id) && id != Guid.Empty)
                return id;
        }

        return null;
    }
}
