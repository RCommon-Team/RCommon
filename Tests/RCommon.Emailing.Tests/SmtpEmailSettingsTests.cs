using FluentAssertions;
using RCommon.Emailing.Smtp;
using Xunit;

namespace RCommon.Emailing.Tests;

public class SmtpEmailSettingsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var settings = new SmtpEmailSettings();

        // Assert
        settings.UserName.Should().BeNull();
        settings.Password.Should().BeNull();
        settings.EnableSsl.Should().BeFalse();
        settings.Port.Should().Be(0);
        settings.Host.Should().BeNull();
        settings.FromEmailDefault.Should().BeNull();
        settings.FromNameDefault.Should().BeNull();
    }

    [Fact]
    public void UserName_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.UserName = "testuser";

        // Assert
        settings.UserName.Should().Be("testuser");
    }

    [Fact]
    public void Password_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.Password = "testpassword";

        // Assert
        settings.Password.Should().Be("testpassword");
    }

    [Fact]
    public void EnableSsl_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.EnableSsl = true;

        // Assert
        settings.EnableSsl.Should().BeTrue();
    }

    [Fact]
    public void Port_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.Port = 587;

        // Assert
        settings.Port.Should().Be(587);
    }

    [Fact]
    public void Host_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.Host = "smtp.test.com";

        // Assert
        settings.Host.Should().Be("smtp.test.com");
    }

    [Fact]
    public void FromEmailDefault_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.FromEmailDefault = "noreply@test.com";

        // Assert
        settings.FromEmailDefault.Should().Be("noreply@test.com");
    }

    [Fact]
    public void FromNameDefault_CanBeSet()
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.FromNameDefault = "Test Sender";

        // Assert
        settings.FromNameDefault.Should().Be("Test Sender");
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Act
        var settings = new SmtpEmailSettings
        {
            UserName = "user",
            Password = "pass",
            EnableSsl = true,
            Port = 465,
            Host = "smtp.example.com",
            FromEmailDefault = "from@example.com",
            FromNameDefault = "Example Sender"
        };

        // Assert
        settings.UserName.Should().Be("user");
        settings.Password.Should().Be("pass");
        settings.EnableSsl.Should().BeTrue();
        settings.Port.Should().Be(465);
        settings.Host.Should().Be("smtp.example.com");
        settings.FromEmailDefault.Should().Be("from@example.com");
        settings.FromNameDefault.Should().Be("Example Sender");
    }

    [Theory]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(2525)]
    public void Port_AcceptsCommonSmtpPorts(int port)
    {
        // Arrange
        var settings = new SmtpEmailSettings();

        // Act
        settings.Port = port;

        // Assert
        settings.Port.Should().Be(port);
    }
}
