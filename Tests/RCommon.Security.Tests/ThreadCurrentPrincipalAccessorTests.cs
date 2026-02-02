using System.Security.Claims;
using System.Security.Principal;
using FluentAssertions;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class ThreadCurrentPrincipalAccessorTests
{
    [Fact]
    public void Principal_WhenThreadPrincipalIsNull_ReturnsNull()
    {
        // Arrange
        var originalPrincipal = Thread.CurrentPrincipal;
        try
        {
            Thread.CurrentPrincipal = null;
            var accessor = new ThreadCurrentPrincipalAccessor();

            // Act
            var result = accessor.Principal;

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            Thread.CurrentPrincipal = originalPrincipal;
        }
    }

    [Fact]
    public void Principal_WhenThreadPrincipalIsClaimsPrincipal_ReturnsIt()
    {
        // Arrange
        var originalPrincipal = Thread.CurrentPrincipal;
        try
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "test");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            Thread.CurrentPrincipal = claimsPrincipal;
            var accessor = new ThreadCurrentPrincipalAccessor();

            // Act
            var result = accessor.Principal;

            // Assert
            result.Should().BeSameAs(claimsPrincipal);
        }
        finally
        {
            Thread.CurrentPrincipal = originalPrincipal;
        }
    }

    [Fact]
    public void Principal_WhenThreadPrincipalIsGenericPrincipal_ReturnsItAsClaimsPrincipal()
    {
        // Arrange
        var originalPrincipal = Thread.CurrentPrincipal;
        try
        {
            var genericIdentity = new GenericIdentity("TestUser");
            var genericPrincipal = new GenericPrincipal(genericIdentity, new[] { "Admin" });
            Thread.CurrentPrincipal = genericPrincipal;
            var accessor = new ThreadCurrentPrincipalAccessor();

            // Act
            var result = accessor.Principal;

            // Assert
            // In modern .NET, GenericPrincipal inherits from ClaimsPrincipal
            result.Should().NotBeNull();
            result.Should().BeSameAs(genericPrincipal);
        }
        finally
        {
            Thread.CurrentPrincipal = originalPrincipal;
        }
    }

    [Fact]
    public void Accessor_ImplementsICurrentPrincipalAccessor()
    {
        // Arrange & Act
        var accessor = new ThreadCurrentPrincipalAccessor();

        // Assert
        accessor.Should().BeAssignableTo<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public void Accessor_DerivesFromCurrentPrincipalAccessorBase()
    {
        // Arrange & Act
        var accessor = new ThreadCurrentPrincipalAccessor();

        // Assert
        accessor.Should().BeAssignableTo<CurrentPrincipalAccessorBase>();
    }

    [Fact]
    public void Change_SetsNewPrincipal()
    {
        // Arrange
        var originalPrincipal = Thread.CurrentPrincipal;
        try
        {
            Thread.CurrentPrincipal = null;
            var accessor = new ThreadCurrentPrincipalAccessor();
            var newClaims = new[] { new Claim(ClaimTypes.Name, "NewUser") };
            var newIdentity = new ClaimsIdentity(newClaims, "test");
            var newPrincipal = new ClaimsPrincipal(newIdentity);

            // Act
            using (accessor.Change(newPrincipal))
            {
                // Assert - inside scope
                accessor.Principal.Should().BeSameAs(newPrincipal);
            }
        }
        finally
        {
            Thread.CurrentPrincipal = originalPrincipal;
        }
    }

    [Fact]
    public void Change_RestoresPreviousPrincipal_WhenDisposed()
    {
        // Arrange
        var originalThreadPrincipal = Thread.CurrentPrincipal;
        try
        {
            var originalClaims = new[] { new Claim(ClaimTypes.Name, "Original") };
            var originalIdentity = new ClaimsIdentity(originalClaims, "test");
            var originalPrincipal = new ClaimsPrincipal(originalIdentity);
            Thread.CurrentPrincipal = originalPrincipal;

            var accessor = new ThreadCurrentPrincipalAccessor();
            var newClaims = new[] { new Claim(ClaimTypes.Name, "New") };
            var newIdentity = new ClaimsIdentity(newClaims, "test");
            var newPrincipal = new ClaimsPrincipal(newIdentity);

            // Act
            using (accessor.Change(newPrincipal))
            {
                accessor.Principal.Should().BeSameAs(newPrincipal);
            }

            // Assert - after dispose, should return to original
            accessor.Principal.Should().BeSameAs(originalPrincipal);
        }
        finally
        {
            Thread.CurrentPrincipal = originalThreadPrincipal;
        }
    }

    [Fact]
    public void Change_CanBeNested()
    {
        // Arrange
        var originalThreadPrincipal = Thread.CurrentPrincipal;
        try
        {
            Thread.CurrentPrincipal = null;
            var accessor = new ThreadCurrentPrincipalAccessor();

            var principal1 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("Level", "1") }, "test"));
            var principal2 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("Level", "2") }, "test"));

            // Act & Assert
            using (accessor.Change(principal1))
            {
                accessor.Principal.Should().BeSameAs(principal1);

                using (accessor.Change(principal2))
                {
                    accessor.Principal.Should().BeSameAs(principal2);
                }

                accessor.Principal.Should().BeSameAs(principal1);
            }

            accessor.Principal.Should().BeNull();
        }
        finally
        {
            Thread.CurrentPrincipal = originalThreadPrincipal;
        }
    }
}
