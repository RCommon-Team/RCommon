using System.Security.Claims;
using FluentAssertions;
using Moq;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class ClaimsTenantIdAccessorTests
{
    private readonly Mock<ICurrentPrincipalAccessor> _mockPrincipalAccessor;

    public ClaimsTenantIdAccessorTests()
    {
        _mockPrincipalAccessor = new Mock<ICurrentPrincipalAccessor>();
    }

    private ClaimsTenantIdAccessor CreateAccessor()
    {
        return new ClaimsTenantIdAccessor(_mockPrincipalAccessor.Object);
    }

    [Fact]
    public void Constructor_WithNullAccessor_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new ClaimsTenantIdAccessor(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTenantId_WhenPrincipalIsNull_ReturnsNull()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WhenTenantClaimExists_ReturnsRawValue()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypesConst.TenantId, "my-tenant") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.GetTenantId();

        // Assert
        result.Should().Be("my-tenant");
    }

    [Fact]
    public void GetTenantId_WhenTenantClaimMissing_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim("other-claim", "value") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WhenTenantClaimEmpty_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypesConst.TenantId, "") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WithGuidValue_ReturnsGuidAsString()
    {
        // Arrange
        var tenantGuid = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypesConst.TenantId, tenantGuid.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.GetTenantId();

        // Assert
        result.Should().Be(tenantGuid.ToString());
    }

    [Fact]
    public void ClaimsTenantIdAccessor_ImplementsITenantIdAccessor()
    {
        // Arrange & Act
        var accessor = CreateAccessor();

        // Assert
        accessor.Should().BeAssignableTo<ITenantIdAccessor>();
    }
}
