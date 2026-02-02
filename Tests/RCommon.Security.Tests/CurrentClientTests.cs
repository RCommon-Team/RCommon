using System.Security.Claims;
using FluentAssertions;
using Moq;
using RCommon.Security.Claims;
using RCommon.Security.Clients;
using Xunit;

namespace RCommon.Security.Tests;

public class CurrentClientTests
{
    private readonly Mock<ICurrentPrincipalAccessor> _mockPrincipalAccessor;

    public CurrentClientTests()
    {
        _mockPrincipalAccessor = new Mock<ICurrentPrincipalAccessor>();
    }

    private CurrentClient CreateCurrentClient()
    {
        return new CurrentClient(_mockPrincipalAccessor.Object);
    }

    [Fact]
    public void Constructor_WithValidAccessor_Succeeds()
    {
        // Act
        var action = () => CreateCurrentClient();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Id_WhenPrincipalIsNull_ReturnsNull()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentClient = CreateCurrentClient();

        // Act
        var result = currentClient.Id;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsAuthenticated_WhenIdIsNull_ReturnsFalse()
    {
        // Arrange
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentClient = CreateCurrentClient();

        // Act
        var result = currentClient.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenIdIsNotNull_ReturnsTrue()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypesConst.ClientId, "client-123") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentClient = CreateCurrentClient();

        // Act
        var result = currentClient.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Id_WithClientIdClaim_ReturnsClientId()
    {
        // Arrange
        var clientId = "my-client-id";
        var claims = new[] { new Claim(ClaimTypesConst.ClientId, clientId) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentClient = CreateCurrentClient();

        // Act
        var result = currentClient.Id;

        // Assert
        result.Should().Be(clientId);
    }

    [Fact]
    public void CurrentClient_ImplementsICurrentClient()
    {
        // Arrange & Act
        var currentClient = CreateCurrentClient();

        // Assert
        currentClient.Should().BeAssignableTo<ICurrentClient>();
    }

    [Fact]
    public void Id_WhenClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim("OtherClaim", "value") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentClient = CreateCurrentClient();

        // Act
        var result = currentClient.Id;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsAuthenticated_DependsOnIdProperty()
    {
        // Arrange - with valid client ID
        var claims = new[] { new Claim(ClaimTypesConst.ClientId, "client") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
        var currentClient = CreateCurrentClient();

        // Assert
        currentClient.Id.Should().NotBeNull();
        currentClient.IsAuthenticated.Should().BeTrue();

        // Arrange - without client ID
        _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        var currentClient2 = CreateCurrentClient();

        // Assert
        currentClient2.Id.Should().BeNull();
        currentClient2.IsAuthenticated.Should().BeFalse();
    }
}
