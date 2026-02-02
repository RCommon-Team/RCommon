using FluentAssertions;
using RCommon.Mediator.Subscribers;
using Xunit;

namespace RCommon.Mediator.Tests.Subscribers;

public class AppNotificationTests
{
    #region Interface Implementation Tests

    [Fact]
    public void IAppNotification_CanBeImplemented()
    {
        // Arrange & Act
        var notification = new TestAppNotification();

        // Assert
        notification.Should().BeAssignableTo<IAppNotification>();
    }

    [Fact]
    public void IAppNotification_ImplementationCanContainProperties()
    {
        // Arrange & Act
        var notification = new TestAppNotification
        {
            Id = 1,
            Message = "Test Message",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        notification.Id.Should().Be(1);
        notification.Message.Should().Be("Test Message");
        notification.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IAppNotification_CanBeUsedAsTypeParameter()
    {
        // Arrange
        var notifications = new List<IAppNotification>();

        // Act
        notifications.Add(new TestAppNotification());
        notifications.Add(new AnotherTestAppNotification());

        // Assert
        notifications.Should().HaveCount(2);
        notifications.Should().AllBeAssignableTo<IAppNotification>();
    }

    [Fact]
    public void IAppNotification_ImplementationsAreDistinct()
    {
        // Arrange
        IAppNotification notification1 = new TestAppNotification();
        IAppNotification notification2 = new AnotherTestAppNotification();

        // Assert
        notification1.GetType().Should().NotBe(notification2.GetType());
    }

    [Fact]
    public void IAppNotification_CanBeUsedWithGenericConstraints()
    {
        // Arrange
        var processor = new NotificationProcessor<TestAppNotification>();

        // Act
        var result = processor.Process(new TestAppNotification { Message = "Test" });

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void IAppNotification_SupportsDerivedTypes()
    {
        // Arrange
        IAppNotification baseNotification = new DerivedTestAppNotification
        {
            Message = "Base Message",
            ExtendedData = "Extended Data"
        };

        // Assert
        baseNotification.Should().BeOfType<DerivedTestAppNotification>();
        ((DerivedTestAppNotification)baseNotification).ExtendedData.Should().Be("Extended Data");
    }

    [Fact]
    public void IAppNotification_CanBeStoredInDictionary()
    {
        // Arrange
        var notificationStore = new Dictionary<string, IAppNotification>();

        // Act
        notificationStore["test1"] = new TestAppNotification { Id = 1 };
        notificationStore["test2"] = new AnotherTestAppNotification { Code = "ABC" };

        // Assert
        notificationStore.Should().HaveCount(2);
        notificationStore["test1"].Should().BeOfType<TestAppNotification>();
        notificationStore["test2"].Should().BeOfType<AnotherTestAppNotification>();
    }

    #endregion

    #region Test Helper Classes

    public class TestAppNotification : IAppNotification
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AnotherTestAppNotification : IAppNotification
    {
        public string Code { get; set; } = string.Empty;
    }

    public class DerivedTestAppNotification : TestAppNotification
    {
        public string ExtendedData { get; set; } = string.Empty;
    }

    public class NotificationProcessor<T> where T : IAppNotification
    {
        public bool Process(T notification)
        {
            return notification != null;
        }
    }

    #endregion
}
