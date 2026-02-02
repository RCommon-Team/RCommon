using FluentAssertions;
using MediatR;
using RCommon.MediatR.Subscribers;
using Xunit;

namespace RCommon.Mediatr.Tests.Subscribers;

public class MediatRNotificationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidNotification_CreatesInstance()
    {
        // Arrange
        var notification = new TestEvent { Message = "Test" };

        // Act
        var mediatRNotification = new MediatRNotification<TestEvent>(notification);

        // Assert
        mediatRNotification.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_StoresNotification()
    {
        // Arrange
        var notification = new TestEvent { Message = "TestMessage" };

        // Act
        var mediatRNotification = new MediatRNotification<TestEvent>(notification);

        // Assert
        mediatRNotification.Notification.Should().BeSameAs(notification);
        mediatRNotification.Notification.Message.Should().Be("TestMessage");
    }

    [Fact]
    public void Constructor_WithNullNotification_StoresNull()
    {
        // Arrange & Act
        var mediatRNotification = new MediatRNotification<TestEvent>(null!);

        // Assert
        mediatRNotification.Notification.Should().BeNull();
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void MediatRNotification_ImplementsIMediatRNotificationOfT()
    {
        // Arrange
        var notification = new TestEvent { Message = "Test" };

        // Act
        var mediatRNotification = new MediatRNotification<TestEvent>(notification);

        // Assert
        mediatRNotification.Should().BeAssignableTo<IMediatRNotification<TestEvent>>();
    }

    [Fact]
    public void MediatRNotification_ImplementsIMediatRNotification()
    {
        // Arrange
        var notification = new TestEvent { Message = "Test" };

        // Act
        var mediatRNotification = new MediatRNotification<TestEvent>(notification);

        // Assert
        mediatRNotification.Should().BeAssignableTo<IMediatRNotification>();
    }

    [Fact]
    public void MediatRNotification_ImplementsINotification()
    {
        // Arrange
        var notification = new TestEvent { Message = "Test" };

        // Act
        var mediatRNotification = new MediatRNotification<TestEvent>(notification);

        // Assert
        mediatRNotification.Should().BeAssignableTo<INotification>();
    }

    #endregion

    #region Notification Property Tests

    [Fact]
    public void Notification_CanBeSet()
    {
        // Arrange
        var notification1 = new TestEvent { Message = "First" };
        var notification2 = new TestEvent { Message = "Second" };
        var mediatRNotification = new MediatRNotification<TestEvent>(notification1);

        // Act
        mediatRNotification.Notification = notification2;

        // Assert
        mediatRNotification.Notification.Should().BeSameAs(notification2);
        mediatRNotification.Notification.Message.Should().Be("Second");
    }

    [Fact]
    public void Notification_CanBeSetToNull()
    {
        // Arrange
        var notification = new TestEvent { Message = "Test" };
        var mediatRNotification = new MediatRNotification<TestEvent>(notification);

        // Act
        mediatRNotification.Notification = null!;

        // Assert
        mediatRNotification.Notification.Should().BeNull();
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void MediatRNotification_WorksWithComplexTypes()
    {
        // Arrange
        var complexNotification = new ComplexEvent
        {
            Id = 42,
            Name = "ComplexEvent",
            Items = new List<string> { "Item1", "Item2", "Item3" }
        };

        // Act
        var mediatRNotification = new MediatRNotification<ComplexEvent>(complexNotification);

        // Assert
        mediatRNotification.Notification.Should().NotBeNull();
        mediatRNotification.Notification.Id.Should().Be(42);
        mediatRNotification.Notification.Name.Should().Be("ComplexEvent");
        mediatRNotification.Notification.Items.Should().HaveCount(3);
    }

    [Fact]
    public void MediatRNotification_WorksWithValueTypes()
    {
        // Arrange
        var value = 123;

        // Act
        var mediatRNotification = new MediatRNotification<int>(value);

        // Assert
        mediatRNotification.Notification.Should().Be(123);
    }

    [Fact]
    public void MediatRNotification_WorksWithStringType()
    {
        // Arrange
        var value = "TestString";

        // Act
        var mediatRNotification = new MediatRNotification<string>(value);

        // Assert
        mediatRNotification.Notification.Should().Be("TestString");
    }

    #endregion

    #region Test Helper Classes

    public class TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ComplexEvent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    #endregion
}
