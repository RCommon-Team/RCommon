using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using Xunit;

namespace RCommon.Core.Tests;

public class InMemoryEventBusTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Assert
        eventBus.Should().NotBeNull();
    }

    #endregion

    #region Subscribe Tests

    [Fact]
    public void Subscribe_RegistersHandler_AndReturnsEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        var result = eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Assert
        result.Should().BeSameAs(eventBus);
    }

    [Fact]
    public void Subscribe_AddsHandlerToServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ISubscriber<TestEvent>) &&
            sd.ImplementationType == typeof(TestEventHandler));
    }

    [Fact]
    public void Subscribe_MultipleHandlers_ForSameEvent_RegistersAll()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        eventBus.Subscribe<TestEvent, TestEventHandler>();
        eventBus.Subscribe<TestEvent, SecondTestEventHandler>();

        // Assert
        services.Count(sd => sd.ServiceType == typeof(ISubscriber<TestEvent>)).Should().Be(2);
    }

    #endregion

    #region SubscribeAllHandledEvents Tests

    [Fact]
    public void SubscribeAllHandledEvents_RegistersAllImplementedInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        eventBus.SubscribeAllHandledEvents<MultiEventHandler>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ISubscriber<TestEvent>) &&
            sd.ImplementationType == typeof(MultiEventHandler));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ISubscriber<AnotherTestEvent>) &&
            sd.ImplementationType == typeof(MultiEventHandler));
    }

    [Fact]
    public void SubscribeAllHandledEvents_ReturnsEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        var result = eventBus.SubscribeAllHandledEvents<MultiEventHandler>();

        // Assert
        result.Should().BeSameAs(eventBus);
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithNoHandlers_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        var act = async () => await eventBus.PublishAsync(testEvent);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithRegisteredHandler_InvokesHandler()
    {
        // Arrange
        var handlerInvoked = false;
        var services = new ServiceCollection();
        services.AddScoped<ISubscriber<TestEvent>>(sp => new ActionTestEventHandler(() => handlerInvoked = true));
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        handlerInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_InvokesAllHandlers()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        var services = new ServiceCollection();
        services.AddScoped<ISubscriber<TestEvent>>(sp => new ActionTestEventHandler(() => handler1Invoked = true));
        services.AddScoped<ISubscriber<TestEvent>>(sp => new ActionTestEventHandler(() => handler2Invoked = true));
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_PassesEventToHandler()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        var services = new ServiceCollection();
        services.AddScoped<ISubscriber<TestEvent>>(sp => new CapturingTestEventHandler(e => receivedEvent = e));
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);
        var testEvent = new TestEvent { Message = "Hello World" };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Message.Should().Be("Hello World");
    }

    #endregion

    #region IEventBus Interface Tests

    [Fact]
    public void InMemoryEventBus_ImplementsIEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Assert
        eventBus.Should().BeAssignableTo<IEventBus>();
    }

    #endregion

    #region Scope Tests

    [Fact]
    public async Task PublishAsync_CreatesNewScope_ForEachPublish()
    {
        // Arrange
        var scopeCount = 0;
        var services = new ServiceCollection();
        services.AddScoped<ISubscriber<TestEvent>>(sp =>
        {
            Interlocked.Increment(ref scopeCount);
            return new TestEventHandler();
        });
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        await eventBus.PublishAsync(new TestEvent());
        await eventBus.PublishAsync(new TestEvent());

        // Assert
        scopeCount.Should().Be(2);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void FluentChaining_Subscribe_AllowsMultipleSubscriptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider, services);

        // Act
        var result = eventBus
            .Subscribe<TestEvent, TestEventHandler>()
            .Subscribe<AnotherTestEvent, AnotherTestEventHandler>();

        // Assert
        result.Should().BeSameAs(eventBus);
        services.Should().Contain(sd => sd.ServiceType == typeof(ISubscriber<TestEvent>));
        services.Should().Contain(sd => sd.ServiceType == typeof(ISubscriber<AnotherTestEvent>));
    }

    #endregion

    #region Test Helper Classes

    public class TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AnotherTestEvent
    {
        public int Value { get; set; }
    }

    public class TestEventHandler : ISubscriber<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class AnotherTestEventHandler : ISubscriber<AnotherTestEvent>
    {
        public Task HandleAsync(AnotherTestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class SecondTestEventHandler : ISubscriber<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class MultiEventHandler : ISubscriber<TestEvent>, ISubscriber<AnotherTestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task HandleAsync(AnotherTestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class ActionTestEventHandler : ISubscriber<TestEvent>
    {
        private readonly Action _action;

        public ActionTestEventHandler(Action action)
        {
            _action = action;
        }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            _action();
            return Task.CompletedTask;
        }
    }

    public class CapturingTestEventHandler : ISubscriber<TestEvent>
    {
        private readonly Action<TestEvent> _capture;

        public CapturingTestEventHandler(Action<TestEvent> capture)
        {
            _capture = capture;
        }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            _capture(@event);
            return Task.CompletedTask;
        }
    }

    #endregion
}
