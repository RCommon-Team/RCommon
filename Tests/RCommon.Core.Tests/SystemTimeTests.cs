using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace RCommon.Core.Tests;

public class SystemTimeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidOptions_InitializesCorrectly()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);

        // Act
        var systemTime = new SystemTime(options);

        // Assert
        systemTime.Should().NotBeNull();
    }

    #endregion

    #region Now Property Tests

    [Fact]
    public void Now_WhenKindIsUtc_ReturnsUtcNow()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);
        var systemTime = new SystemTime(options);

        // Act
        var before = DateTime.UtcNow;
        var result = systemTime.Now;
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Now_WhenKindIsLocal_ReturnsLocalNow()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Local);
        var systemTime = new SystemTime(options);

        // Act
        var before = DateTime.Now;
        var result = systemTime.Now;
        var after = DateTime.Now;

        // Assert
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Now_WhenKindIsUnspecified_ReturnsLocalNow()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Unspecified);
        var systemTime = new SystemTime(options);

        // Act
        var before = DateTime.Now;
        var result = systemTime.Now;
        var after = DateTime.Now;

        // Assert
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Kind Property Tests

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Kind_ReturnsConfiguredKind(DateTimeKind expectedKind)
    {
        // Arrange
        var options = CreateOptions(expectedKind);
        var systemTime = new SystemTime(options);

        // Act
        var result = systemTime.Kind;

        // Assert
        result.Should().Be(expectedKind);
    }

    #endregion

    #region SupportsMultipleTimezone Property Tests

    [Fact]
    public void SupportsMultipleTimezone_WhenKindIsUtc_ReturnsTrue()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);
        var systemTime = new SystemTime(options);

        // Act
        var result = systemTime.SupportsMultipleTimezone;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsMultipleTimezone_WhenKindIsLocal_ReturnsFalse()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Local);
        var systemTime = new SystemTime(options);

        // Act
        var result = systemTime.SupportsMultipleTimezone;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SupportsMultipleTimezone_WhenKindIsUnspecified_ReturnsFalse()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Unspecified);
        var systemTime = new SystemTime(options);

        // Act
        var result = systemTime.SupportsMultipleTimezone;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Normalize Tests - Same Kind

    [Fact]
    public void Normalize_WhenKindIsUnspecified_ReturnsOriginalDateTime()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Unspecified);
        var systemTime = new SystemTime(options);
        var inputDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = systemTime.Normalize(inputDateTime);

        // Assert
        result.Should().Be(inputDateTime);
    }

    [Fact]
    public void Normalize_WhenKindsMatch_ReturnsOriginalDateTime()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);
        var systemTime = new SystemTime(options);
        var inputDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = systemTime.Normalize(inputDateTime);

        // Assert
        result.Should().Be(inputDateTime);
    }

    #endregion

    #region Normalize Tests - Local to Utc

    [Fact]
    public void Normalize_WhenKindIsUtcAndInputIsLocal_ConvertsToUtc()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);
        var systemTime = new SystemTime(options);
        var localDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Local);

        // Act
        var result = systemTime.Normalize(localDateTime);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(localDateTime.ToUniversalTime());
    }

    #endregion

    #region Normalize Tests - Utc to Local

    [Fact]
    public void Normalize_WhenKindIsLocalAndInputIsUtc_ConvertsToLocal()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Local);
        var systemTime = new SystemTime(options);
        var utcDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = systemTime.Normalize(utcDateTime);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Local);
        result.Should().Be(utcDateTime.ToLocalTime());
    }

    #endregion

    #region Normalize Tests - Unspecified Input

    [Fact]
    public void Normalize_WhenKindIsUtcAndInputIsUnspecified_SpecifiesKindAsUtc()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);
        var systemTime = new SystemTime(options);
        var unspecifiedDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Unspecified);

        // Act
        var result = systemTime.Normalize(unspecifiedDateTime);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Year.Should().Be(2023);
        result.Month.Should().Be(6);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(10);
        result.Minute.Should().Be(30);
    }

    [Fact]
    public void Normalize_WhenKindIsLocalAndInputIsUnspecified_SpecifiesKindAsLocal()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Local);
        var systemTime = new SystemTime(options);
        var unspecifiedDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Unspecified);

        // Act
        var result = systemTime.Normalize(unspecifiedDateTime);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Local);
        result.Year.Should().Be(2023);
        result.Month.Should().Be(6);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(10);
        result.Minute.Should().Be(30);
    }

    #endregion

    #region ISystemTime Interface Tests

    [Fact]
    public void SystemTime_ImplementsISystemTime()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);

        // Act
        var systemTime = new SystemTime(options);

        // Assert
        systemTime.Should().BeAssignableTo<ISystemTime>();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Normalize_WithMinDateTime_HandlesCorrectly()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Utc);
        var systemTime = new SystemTime(options);
        var minDateTime = DateTime.MinValue;

        // Act
        var result = systemTime.Normalize(minDateTime);

        // Assert - MinValue with Unspecified kind will be specified as Utc
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Normalize_WithMaxDateTime_HandlesCorrectly()
    {
        // Arrange
        var options = CreateOptions(DateTimeKind.Unspecified);
        var systemTime = new SystemTime(options);
        var maxDateTime = DateTime.MaxValue;

        // Act
        var result = systemTime.Normalize(maxDateTime);

        // Assert
        result.Should().Be(maxDateTime);
    }

    #endregion

    #region Helper Methods

    private static IOptions<SystemTimeOptions> CreateOptions(DateTimeKind kind)
    {
        var options = new SystemTimeOptions { Kind = kind };
        var mockOptions = new Mock<IOptions<SystemTimeOptions>>();
        mockOptions.Setup(x => x.Value).Returns(options);
        return mockOptions.Object;
    }

    #endregion
}
