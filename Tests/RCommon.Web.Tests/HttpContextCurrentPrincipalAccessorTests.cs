using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RCommon.Security.Claims;
using RCommon.Web.Security;
using Xunit;

namespace RCommon.Web.Tests;

public class HttpContextCurrentPrincipalAccessorTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public HttpContextCurrentPrincipalAccessorTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    }

    private HttpContextCurrentPrincipalAccessor CreateAccessor()
    {
        return new HttpContextCurrentPrincipalAccessor(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void Constructor_WithNullAccessor_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new HttpContextCurrentPrincipalAccessor(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Principal_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.Principal;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Principal_WhenHttpContextHasUser_ReturnsUser()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.Principal;

        // Assert
        result.Should().BeSameAs(principal);
    }

    [Fact]
    public void Principal_WhenHttpContextHasDefaultUser_ReturnsDefaultPrincipal()
    {
        // Arrange — DefaultHttpContext.User is never null, it returns an empty ClaimsPrincipal
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var accessor = CreateAccessor();

        // Act
        var result = accessor.Principal;

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Change_TemporarilyOverridesPrincipal_RestoresOnDispose()
    {
        // Arrange
        var originalClaims = new[] { new Claim(ClaimTypes.Name, "original") };
        var originalIdentity = new ClaimsIdentity(originalClaims, "test");
        var originalPrincipal = new ClaimsPrincipal(originalIdentity);
        var httpContext = new DefaultHttpContext { User = originalPrincipal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var accessor = CreateAccessor();

        var overrideClaims = new[] { new Claim(ClaimTypes.Name, "override") };
        var overrideIdentity = new ClaimsIdentity(overrideClaims, "test");
        var overridePrincipal = new ClaimsPrincipal(overrideIdentity);

        // Act & Assert — override
        using (accessor.Change(overridePrincipal))
        {
            accessor.Principal.Should().BeSameAs(overridePrincipal);
        }

        // After dispose — restored to original
        accessor.Principal.Should().BeSameAs(originalPrincipal);
    }

    [Fact]
    public void ImplementsICurrentPrincipalAccessor()
    {
        // Arrange & Act
        var accessor = CreateAccessor();

        // Assert
        accessor.Should().BeAssignableTo<ICurrentPrincipalAccessor>();
    }
}
