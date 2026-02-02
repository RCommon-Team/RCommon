using System.Net.Mail;
using FluentAssertions;
using RCommon.Emailing;
using Xunit;

namespace RCommon.Emailing.Tests;

public class EmailEventArgsTests
{
    [Fact]
    public void Constructor_WithMailMessage_SetsProperty()
    {
        // Arrange
        var message = new MailMessage("from@test.com", "to@test.com", "Subject", "Body");

        // Act
        var args = new EmailEventArgs(message);

        // Assert
        args.MailMessage.Should().BeSameAs(message);
    }

    [Fact]
    public void Constructor_WithNullMailMessage_DoesNotThrow()
    {
        // Act
        var action = () => new EmailEventArgs(null!);

        // Assert - EmailEventArgs doesn't validate null, just stores the reference
        action.Should().NotThrow();
    }

    [Fact]
    public void MailMessage_IsReadOnly()
    {
        // Arrange
        var message = new MailMessage();
        var args = new EmailEventArgs(message);

        // Assert
        args.MailMessage.Should().BeSameAs(message);
        // Property has no setter, so this verifies read-only nature
        typeof(EmailEventArgs).GetProperty("MailMessage")!.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void Constructor_PreservesMessageDetails()
    {
        // Arrange
        var message = new MailMessage
        {
            From = new MailAddress("sender@test.com", "Sender Name"),
            Subject = "Test Subject",
            Body = "Test Body",
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress("recipient@test.com", "Recipient Name"));

        // Act
        var args = new EmailEventArgs(message);

        // Assert
        args.MailMessage.From!.Address.Should().Be("sender@test.com");
        args.MailMessage.Subject.Should().Be("Test Subject");
        args.MailMessage.Body.Should().Be("Test Body");
        args.MailMessage.IsBodyHtml.Should().BeTrue();
        args.MailMessage.To.Should().HaveCount(1);
    }

    [Fact]
    public void Class_DerivesFromEventArgs()
    {
        // Arrange
        var message = new MailMessage();

        // Act
        var args = new EmailEventArgs(message);

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }
}
