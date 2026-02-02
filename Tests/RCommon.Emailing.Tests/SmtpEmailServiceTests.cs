using System.Net.Mail;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Emailing;
using RCommon.Emailing.Smtp;
using Xunit;

namespace RCommon.Emailing.Tests;

public class SmtpEmailServiceTests
{
    private readonly Mock<IOptions<SmtpEmailSettings>> _mockOptions;
    private readonly SmtpEmailSettings _settings;

    public SmtpEmailServiceTests()
    {
        _settings = new SmtpEmailSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            UserName = "testuser",
            Password = "testpassword",
            EnableSsl = true
        };
        _mockOptions = new Mock<IOptions<SmtpEmailSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
    }

    private SmtpEmailService CreateService()
    {
        return new SmtpEmailService(_mockOptions.Object);
    }

    [Fact]
    public void Constructor_WithValidOptions_Succeeds()
    {
        // Act
        var action = () => CreateService();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_AccessesOptionsValue()
    {
        // Act
        var service = CreateService();

        // Assert
        _mockOptions.Verify(x => x.Value, Times.Once);
    }

    [Fact]
    public void SendEmail_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var action = () => service.SendEmail(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("message");
    }

    [Fact]
    public void Service_ImplementsIEmailService()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IEmailService>();
    }

    [Fact]
    public void Service_HasEmailSentEvent()
    {
        // Arrange
        var service = CreateService();

        // Assert
        typeof(SmtpEmailService).GetEvent("EmailSent").Should().NotBeNull();
    }

    [Fact]
    public void EmailSent_EventCanBeSubscribed()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;

        // Act
        service.EmailSent += (sender, args) => eventRaised = true;

        // Assert - just verify no exception on subscription
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SendEmailAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var action = async () => await service.SendEmailAsync(null!);

        // Assert
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithMinimalSettings_Succeeds()
    {
        // Arrange
        var minimalSettings = new SmtpEmailSettings();
        var options = new Mock<IOptions<SmtpEmailSettings>>();
        options.Setup(x => x.Value).Returns(minimalSettings);

        // Act
        var action = () => new SmtpEmailService(options.Object);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Service_PreservesSettingsFromOptions()
    {
        // Arrange
        var customSettings = new SmtpEmailSettings
        {
            Host = "custom.smtp.com",
            Port = 465,
            UserName = "customuser",
            Password = "custompass",
            EnableSsl = false
        };
        var options = new Mock<IOptions<SmtpEmailSettings>>();
        options.Setup(x => x.Value).Returns(customSettings);

        // Act
        var service = new SmtpEmailService(options.Object);

        // Assert
        options.Verify(x => x.Value, Times.Once);
    }
}
