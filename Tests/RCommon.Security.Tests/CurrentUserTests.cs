using System.Security.Claims;
using FluentAssertions;
using Moq;
using RCommon.Security.Claims;
using RCommon.Security.Users;
using Xunit;

namespace RCommon.Security.Tests;

public class CurrentUserTests
{
    private readonly Mock<ICurrentPrincipalAccessor> _mockPrincipalAccessor;

    public CurrentUserTests()
    {
        _mockPrincipalAccessor = new Mock<ICurrentPrincipalAccessor>();
    }

    private CurrentUser CreateCurrentUser()
    {
        return new CurrentUser(_mockPrincipalAccessor.Object);
    }

    [Fact]
    public void Constructor_WithValidAccessor_Succeeds()
    {
        // Act
        var action = () => CreateCurrentUser();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void IsAuthenticated_WhenIdIsNull_ReturnsFalse()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenIdHasValue_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Id_WhenPrincipalIsNull_ReturnsNull()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.Id;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TenantId_WhenPrincipalIsNull_ReturnsNull()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.TenantId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TenantId_WhenTenantClaimExists_ReturnsRawStringValue()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypesConst.TenantId, "my-tenant") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.TenantId;

        // Assert
        result.Should().Be("my-tenant");
    }

    [Fact]
    public void TenantId_WithGuidValue_ReturnsGuidAsString()
    {
        // Arrange
        var tenantGuid = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypesConst.TenantId, tenantGuid.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.TenantId;

        // Assert
        result.Should().Be(tenantGuid.ToString());
    }

    [Fact]
    public void Roles_WhenPrincipalIsNull_ReturnsEmptyArray()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.Roles;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Roles_WithMultipleRoles_ReturnsDistinctRoles()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypesConst.Role, "Admin"),
            new Claim(ClaimTypesConst.Role, "User"),
            new Claim(ClaimTypesConst.Role, "Admin") // Duplicate
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.Roles;

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Admin");
        result.Should().Contain("User");
    }

    [Fact]
    public void FindClaim_WhenClaimExists_ReturnsClaim()
    {
        // Arrange
        var claims = new[] { new Claim("CustomClaim", "CustomValue") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.FindClaim("CustomClaim");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("CustomValue");
    }

    [Fact]
    public void FindClaim_WhenClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim("OtherClaim", "Value") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.FindClaim("NonExistentClaim");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindClaim_WhenPrincipalIsNull_ReturnsNull()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.FindClaim("AnyClaim");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindClaims_WhenClaimsExist_ReturnsMatchingClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("TestClaim", "Value1"),
            new Claim("TestClaim", "Value2"),
            new Claim("OtherClaim", "OtherValue")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.FindClaims("TestClaim");

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Value).Should().Contain("Value1");
        result.Select(c => c.Value).Should().Contain("Value2");
    }

    [Fact]
    public void FindClaims_WhenPrincipalIsNull_ReturnsEmptyArray()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.FindClaims("AnyClaim");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllClaims_ReturnsAllClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("Claim1", "Value1"),
            new Claim("Claim2", "Value2"),
            new Claim("Claim3", "Value3")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.GetAllClaims();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void GetAllClaims_WhenPrincipalIsNull_ReturnsEmptyArray()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentUser = CreateCurrentUser();

        // Act
        var result = currentUser.GetAllClaims();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CurrentUser_ImplementsICurrentUser()
    {
        // Arrange & Act
        var currentUser = CreateCurrentUser();

        // Assert
        currentUser.Should().BeAssignableTo<ICurrentUser>();
    }
}
