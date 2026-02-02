using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class SystemTimeOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultsKindToUnspecified()
    {
        // Arrange & Act
        var options = new SystemTimeOptions();

        // Assert
        options.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    #endregion

    #region Kind Property Tests

    [Fact]
    public void Kind_CanBeSetToUtc()
    {
        // Arrange
        var options = new SystemTimeOptions();

        // Act
        options.Kind = DateTimeKind.Utc;

        // Assert
        options.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Kind_CanBeSetToLocal()
    {
        // Arrange
        var options = new SystemTimeOptions();

        // Act
        options.Kind = DateTimeKind.Local;

        // Assert
        options.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void Kind_CanBeSetToUnspecified()
    {
        // Arrange
        var options = new SystemTimeOptions { Kind = DateTimeKind.Utc };

        // Act
        options.Kind = DateTimeKind.Unspecified;

        // Assert
        options.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Kind_AcceptsAllDateTimeKindValues(DateTimeKind kind)
    {
        // Arrange
        var options = new SystemTimeOptions();

        // Act
        options.Kind = kind;

        // Assert
        options.Kind.Should().Be(kind);
    }

    #endregion

    #region ISystemTimeOptions Interface Tests

    [Fact]
    public void SystemTimeOptions_ImplementsISystemTimeOptions()
    {
        // Arrange & Act
        var options = new SystemTimeOptions();

        // Assert
        options.Should().BeAssignableTo<ISystemTimeOptions>();
    }

    [Fact]
    public void Kind_AsISystemTimeOptions_ReturnsCorrectValue()
    {
        // Arrange
        ISystemTimeOptions options = new SystemTimeOptions { Kind = DateTimeKind.Utc };

        // Act
        var result = options.Kind;

        // Assert
        result.Should().Be(DateTimeKind.Utc);
    }

    #endregion
}
