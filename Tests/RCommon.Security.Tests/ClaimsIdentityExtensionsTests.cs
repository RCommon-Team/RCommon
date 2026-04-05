using System.Security.Claims;
using FluentAssertions;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class ClaimsIdentityExtensionsTests : IDisposable
{
    public ClaimsIdentityExtensionsTests()
    {
        ClaimTypesConst.Reset();
    }

    public void Dispose()
    {
        ClaimTypesConst.Reset();
    }

    [Fact]
    public void FindUserId_WithGuidClaim_ReturnsGuidString()
    {
        var guid = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, guid);

        var result = principal.FindUserId();

        result.Should().Be(guid);
    }

    [Fact]
    public void FindUserId_WithIntegerClaim_ReturnsIntegerString()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "12345");

        var result = principal.FindUserId();

        result.Should().Be("12345");
    }

    [Fact]
    public void FindUserId_WithAuth0StyleClaim_ReturnsFullString()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "auth0|abc123def456");

        var result = principal.FindUserId();

        result.Should().Be("auth0|abc123def456");
    }

    [Fact]
    public void FindUserId_WhenClaimMissing_ReturnsNull()
    {
        var principal = CreatePrincipal("other-claim", "value");

        var result = principal.FindUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void FindUserId_WhenClaimValueEmpty_ReturnsNull()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "");

        var result = principal.FindUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void FindUserId_WhenClaimValueWhitespace_ReturnsNull()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "   ");

        var result = principal.FindUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void FindUserId_OnIIdentity_WithStringClaim_ReturnsString()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") }, "test");

        var result = ((System.Security.Principal.IIdentity)identity).FindUserId();

        result.Should().Be("user-42");
    }

    [Fact]
    public void FindUserId_OnIIdentity_WhenClaimMissing_ReturnsNull()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("other", "value") }, "test");

        var result = ((System.Security.Principal.IIdentity)identity).FindUserId();

        result.Should().BeNull();
    }

    private static ClaimsPrincipal CreatePrincipal(string claimType, string claimValue)
    {
        var claims = new[] { new Claim(claimType, claimValue) };
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}
