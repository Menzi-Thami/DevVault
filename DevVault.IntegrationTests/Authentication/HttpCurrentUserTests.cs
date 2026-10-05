using System.Security.Claims;
using DevVault.API.Authentication;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.Authentication;

/// <summary>The claim rule shared by <see cref="HttpCurrentUser"/> and the bearer OnTokenValidated check.</summary>
public sealed class HttpCurrentUserTests
{
    private static readonly Guid Oid = Guid.Parse("8c7a3f52-1b9d-4c0e-9a51-3f2d6e4b7a10");
    private static readonly Guid Sub = Guid.Parse("d41e0b77-52c3-4f8a-b6e9-0a9c3d5f2b84");

    [Fact]
    public void PrefersOid_OverSub() =>
        HttpCurrentUser.TryGetUserId(Authenticated(("oid", Oid.ToString()), ("sub", Sub.ToString()))).ShouldBe(Oid);

    [Fact]
    public void FallsBackToAGuidSub() =>
        HttpCurrentUser.TryGetUserId(Authenticated(("sub", Sub.ToString()))).ShouldBe(Sub);

    [Theory]
    [InlineData("sub", "AAAAAAAAAAAAAAAAAAAAAIkzqFVrSaSaFHy782bbtaQ")]   // Entra's pairwise sub is not a GUID
    [InlineData("oid", "00000000-0000-0000-0000-000000000000")]
    [InlineData("name", "8c7a3f52-1b9d-4c0e-9a51-3f2d6e4b7a10")]
    public void RejectsTokensWithoutAUsableUserId(string claimType, string value) =>
        HttpCurrentUser.TryGetUserId(Authenticated((claimType, value))).ShouldBeNull();

    [Fact]
    public void RejectsUnauthenticatedPrincipals() =>
        HttpCurrentUser.TryGetUserId(new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", Oid.ToString())])))
            .ShouldBeNull();

    [Fact]
    public void UserId_ReadsTheCurrentRequest()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = Authenticated(("oid", Oid.ToString())) }
        };

        new HttpCurrentUser(accessor).UserId.ShouldBe(Oid);
    }

    [Fact]
    public void UserId_WithoutAUser_Throws() =>
        Should.Throw<InvalidOperationException>(() => new HttpCurrentUser(new HttpContextAccessor()).UserId);

    private static ClaimsPrincipal Authenticated(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Bearer"));
}
