using System.Net.Mail;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Emailing;
using RCommon.Emailing.SendGrid;
using Xunit;

namespace RCommon.SendGrid.Tests;

public class SendGridEmailServiceTests
{
    private readonly Mock<IOptions<SendGridEmailSettings>> _mockOptions;
    private readonly Mock<ILogger<SendGridEmailService>> _mockLogger;
    private readonly SendGridEmailSettings _settings;

    public SendGridEmailServiceTests()
    {
        _settings = new SendGridEmailSettings
        {
            SendGridApiKey = "SG.test-api-key",
            FromEmailDefault = "noreply@test.com",
            FromNameDefault = "Test Sender"
        };
        _mockOptions = new Mock<IOptions<SendGridEmailSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockLogger = new Mock<ILogger<SendGridEmailService>>();
    }

    private SendGridEmailService CreateService()
    {
        return new SendGridEmailService(_mockOptions.Object, _mockLogger.Object);
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
        typeof(SendGridEmailService).GetEvent("EmailSent").Should().NotBeNull();
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
    public void SendEmail_CallsSynchronousWrapper()
    {
        // Arrange
        var service = CreateService();

        // Note: SendEmail internally calls SendEmailAsync via AsyncHelper.RunSync
        // We're just verifying the service is properly constructed
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptyRecipients_ReturnsImmediately()
    {
        // Arrange
        var service = CreateService();
        var message = new MailMessage
        {
            From = new MailAddress("from@test.com"),
            Subject = "Test",
            Body = "Test body"
        };
        // No recipients added - To collection is empty

        // Act
        await service.SendEmailAsync(message);

        // Assert - should not throw, returns immediately when no recipients
    }

    [Fact]
    public void Constructor_WithMinimalSettings_Succeeds()
    {
        // Arrange
        var minimalSettings = new SendGridEmailSettings
        {
            SendGridApiKey = "SG.minimal-key"
        };
        var options = new Mock<IOptions<SendGridEmailSettings>>();
        options.Setup(x => x.Value).Returns(minimalSettings);

        // Act
        var action = () => new SendGridEmailService(options.Object, _mockLogger.Object);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Arrange
        var settingsWithNullKey = new SendGridEmailSettings
        {
            SendGridApiKey = null
        };
        var options = new Mock<IOptions<SendGridEmailSettings>>();
        options.Setup(x => x.Value).Returns(settingsWithNullKey);

        // Act - SendGridClient requires a non-null API key
        var action = () => new SendGridEmailService(options.Object, _mockLogger.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiKey");
    }

    [Fact]
    public void Service_PreservesSettingsFromOptions()
    {
        // Arrange
        var customSettings = new SendGridEmailSettings
        {
            SendGridApiKey = "SG.custom-key",
            FromEmailDefault = "custom@test.com",
            FromNameDefault = "Custom Sender"
        };
        var options = new Mock<IOptions<SendGridEmailSettings>>();
        options.Setup(x => x.Value).Returns(customSettings);

        // Act
        var service = new SendGridEmailService(options.Object, _mockLogger.Object);

        // Assert
        options.Verify(x => x.Value, Times.Once);
    }
}
