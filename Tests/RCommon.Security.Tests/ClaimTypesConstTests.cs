using System.Security.Claims;
using FluentAssertions;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class ClaimTypesConstTests : IDisposable
{
    public ClaimTypesConstTests()
    {
        ClaimTypesConst.Reset();
    }

    public void Dispose()
    {
        ClaimTypesConst.Reset();
    }

    [Fact]
    public void Defaults_MatchStandardClaimTypes()
    {
        ClaimTypesConst.UserId.Should().Be(ClaimTypes.NameIdentifier);
        ClaimTypesConst.UserName.Should().Be(ClaimTypes.Name);
        ClaimTypesConst.Name.Should().Be(ClaimTypes.GivenName);
        ClaimTypesConst.SurName.Should().Be(ClaimTypes.Surname);
        ClaimTypesConst.Role.Should().Be(ClaimTypes.Role);
        ClaimTypesConst.Email.Should().Be(ClaimTypes.Email);
        ClaimTypesConst.TenantId.Should().Be("tenantid");
        ClaimTypesConst.ClientId.Should().Be("client_id");
    }

    [Fact]
    public void Configure_AppliesCustomValues()
    {
        ClaimTypesConst.Configure(options =>
        {
            options.UserId = "sub";
            options.Role = "roles";
            options.TenantId = "tenant_id";
        });

        ClaimTypesConst.UserId.Should().Be("sub");
        ClaimTypesConst.Role.Should().Be("roles");
        ClaimTypesConst.TenantId.Should().Be("tenant_id");
        // Unchanged values keep defaults
        ClaimTypesConst.Email.Should().Be(ClaimTypes.Email);
    }

    [Fact]
    public void Configure_CalledTwice_ThrowsInvalidOperationException()
    {
        ClaimTypesConst.Configure(options => { options.UserId = "sub"; });

        var action = () => ClaimTypesConst.Configure(options => { options.Role = "roles"; });

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Configure_WithNullAction_ThrowsArgumentNullException()
    {
        var action = () => ClaimTypesConst.Configure(null!);

        action.Should().Throw<ArgumentNullException>();
    }
}
