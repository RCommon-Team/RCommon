using FluentAssertions;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests;

public class EventProductionExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Event production failed";

        // Act
        var exception = new EventProductionException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndParams_FormatsMessage()
    {
        // Arrange
        var message = "Failed to produce event: {0}";
        var eventName = "OrderCreated";

        // Act
        var exception = new EventProductionException(message, new object[] { eventName });

        // Assert
        exception.Message.Should().Be("Failed to produce event: OrderCreated");
    }

    [Fact]
    public void Constructor_WithMessageExceptionAndParams_SetsAllProperties()
    {
        // Arrange
        var message = "Error in {0}";
        var innerException = new InvalidOperationException("Inner error");
        var producerName = "KafkaProducer";

        // Act
        var exception = new EventProductionException(message, innerException, new object[] { producerName });

        // Assert
        exception.Message.Should().Be("Error in KafkaProducer");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void EventProductionException_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new EventProductionException("Test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void EventProductionException_InheritsFromBaseApplicationException()
    {
        // Arrange & Act
        var exception = new EventProductionException("Test");

        // Assert
        exception.Should().BeAssignableTo<BaseApplicationException>();
    }

    [Fact]
    public void EventProductionException_InheritsEnvironmentInfo()
    {
        // Arrange & Act
        var exception = new EventProductionException("Test");

        // Assert
        exception.MachineName.Should().NotBeNullOrEmpty();
        exception.AppDomainName.Should().NotBeNullOrEmpty();
        exception.CreatedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Exception Throwing Tests

    [Fact]
    public void EventProductionException_CanBeThrown()
    {
        // Arrange & Act
        Action act = () => throw new EventProductionException("Production failed");

        // Assert
        act.Should().Throw<EventProductionException>().WithMessage("Production failed");
    }

    [Fact]
    public void EventProductionException_CanBeCaughtAsGeneralException()
    {
        // Arrange & Act
        EventProductionException? caughtException = null;
        try
        {
            throw new EventProductionException("Test");
        }
        catch (GeneralException ex)
        {
            caughtException = ex as EventProductionException;
        }

        // Assert
        caughtException.Should().NotBeNull();
    }

    #endregion

    #region Severity Tests

    [Fact]
    public void Severity_DefaultsToHigh()
    {
        // Arrange & Act
        var exception = new EventProductionException("Test");

        // Assert
        exception.Severity.Should().Be(SeverityOptions.High);
    }

    [Fact]
    public void Severity_CanBeModified()
    {
        // Arrange
        var exception = new EventProductionException("Test");

        // Act
        exception.Severity = SeverityOptions.Critical;

        // Assert
        exception.Severity.Should().Be(SeverityOptions.Critical);
    }

    #endregion

    #region DebugMessage Tests

    [Fact]
    public void DebugMessage_WithParams_FormatsCorrectly()
    {
        // Arrange
        var exception = new EventProductionException(
            "Event {0} failed at {1}",
            new object[] { "UserCreated", "2023-06-15" });

        // Act
        var debugMessage = exception.DebugMessage;

        // Assert
        debugMessage.Should().Be("Event UserCreated failed at 2023-06-15");
    }

    #endregion

    #region Multiple Parameters Tests

    [Fact]
    public void Constructor_WithMultipleParams_FormatsAllCorrectly()
    {
        // Arrange
        var message = "Producer {0} failed for event {1} with status {2}";

        // Act
        var exception = new EventProductionException(message, new object[] { "RabbitMQ", "OrderPlaced", 500 });

        // Assert
        exception.Message.Should().Be("Producer RabbitMQ failed for event OrderPlaced with status 500");
    }

    #endregion

    #region InnerException Tests

    [Fact]
    public void InnerException_IsPreserved()
    {
        // Arrange
        var innerException = new TimeoutException("Connection timed out");

        // Act
        var exception = new EventProductionException(
            "Failed: {0}",
            innerException,
            new object[] { "timeout" });

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.Message.Should().Be("Connection timed out");
    }

    #endregion
}
