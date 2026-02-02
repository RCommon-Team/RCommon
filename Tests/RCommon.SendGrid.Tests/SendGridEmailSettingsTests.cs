using FluentAssertions;
using RCommon.Emailing.SendGrid;
using Xunit;

namespace RCommon.SendGrid.Tests;

public class SendGridEmailSettingsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var settings = new SendGridEmailSettings();

        // Assert
        settings.SendGridApiKey.Should().BeNull();
        settings.FromEmailDefault.Should().BeNull();
        settings.FromNameDefault.Should().BeNull();
    }

    [Fact]
    public void SendGridApiKey_CanBeSet()
    {
        // Arrange
        var settings = new SendGridEmailSettings();

        // Act
        settings.SendGridApiKey = "SG.test-api-key";

        // Assert
        settings.SendGridApiKey.Should().Be("SG.test-api-key");
    }

    [Fact]
    public void FromEmailDefault_CanBeSet()
    {
        // Arrange
        var settings = new SendGridEmailSettings();

        // Act
        settings.FromEmailDefault = "noreply@test.com";

        // Assert
        settings.FromEmailDefault.Should().Be("noreply@test.com");
    }

    [Fact]
    public void FromNameDefault_CanBeSet()
    {
        // Arrange
        var settings = new SendGridEmailSettings();

        // Act
        settings.FromNameDefault = "Test Sender";

        // Assert
        settings.FromNameDefault.Should().Be("Test Sender");
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Act
        var settings = new SendGridEmailSettings
        {
            SendGridApiKey = "SG.complete-key",
            FromEmailDefault = "from@example.com",
            FromNameDefault = "Example Sender"
        };

        // Assert
        settings.SendGridApiKey.Should().Be("SG.complete-key");
        settings.FromEmailDefault.Should().Be("from@example.com");
        settings.FromNameDefault.Should().Be("Example Sender");
    }

    [Fact]
    public void SendGridApiKey_AcceptsNullValue()
    {
        // Arrange
        var settings = new SendGridEmailSettings { SendGridApiKey = "SG.key" };

        // Act
        settings.SendGridApiKey = null;

        // Assert
        settings.SendGridApiKey.Should().BeNull();
    }

    [Fact]
    public void FromEmailDefault_AcceptsNullValue()
    {
        // Arrange
        var settings = new SendGridEmailSettings { FromEmailDefault = "test@test.com" };

        // Act
        settings.FromEmailDefault = null;

        // Assert
        settings.FromEmailDefault.Should().BeNull();
    }

    [Fact]
    public void FromNameDefault_AcceptsNullValue()
    {
        // Arrange
        var settings = new SendGridEmailSettings { FromNameDefault = "Test" };

        // Act
        settings.FromNameDefault = null;

        // Assert
        settings.FromNameDefault.Should().BeNull();
    }

    [Fact]
    public void Properties_AreNullable()
    {
        // Assert
        typeof(SendGridEmailSettings).GetProperty("SendGridApiKey")!.PropertyType.Should().Be(typeof(string));
        typeof(SendGridEmailSettings).GetProperty("FromEmailDefault")!.PropertyType.Should().Be(typeof(string));
        typeof(SendGridEmailSettings).GetProperty("FromNameDefault")!.PropertyType.Should().Be(typeof(string));
    }
}
