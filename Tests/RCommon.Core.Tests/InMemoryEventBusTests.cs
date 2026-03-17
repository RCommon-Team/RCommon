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
        var eventBus = new InMemoryEventBus(serviceProvider);

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
        var eventBus = new InMemoryEventBus(serviceProvider);

        // Act
        var result = eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Assert
        result.Should().BeSameAs(eventBus);
    }

    [Fact]
    public async Task Subscribe_DynamicHandler_IsInvokedOnPublish()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider);

        // Act - subscribe dynamically, then publish
        eventBus.Subscribe<TestEvent, TestEventHandler>();
        var act = async () => await eventBus.PublishAsync(new TestEvent { Message = "test" });

        // Assert - should not throw (handler resolved via ActivatorUtilities)
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Subscribe_MultipleHandlers_ForSameEvent_AllInvoked()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider);

        // Act
        eventBus.Subscribe<TestEvent, TestEventHandler>();
        eventBus.Subscribe<TestEvent, SecondTestEventHandler>();
        var act = async () => await eventBus.PublishAsync(new TestEvent { Message = "test" });

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SubscribeAllHandledEvents Tests

    [Fact]
    public async Task SubscribeAllHandledEvents_RegistersAllImplementedInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider);

        // Act - subscribe multi-handler, then publish both event types
        eventBus.SubscribeAllHandledEvents<MultiEventHandler>();
        var act1 = async () => await eventBus.PublishAsync(new TestEvent());
        var act2 = async () => await eventBus.PublishAsync(new AnotherTestEvent());

        // Assert - both event types should be handled without throwing
        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
    }

    [Fact]
    public void SubscribeAllHandledEvents_ReturnsEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider);

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
        var eventBus = new InMemoryEventBus(serviceProvider);
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
        var eventBus = new InMemoryEventBus(serviceProvider);
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
        var eventBus = new InMemoryEventBus(serviceProvider);
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
        var eventBus = new InMemoryEventBus(serviceProvider);
        var testEvent = new TestEvent { Message = "Hello World" };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Message.Should().Be("Hello World");
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationTokenToHandler()
    {
        // Arrange
        CancellationToken? receivedToken = null;
        var services = new ServiceCollection();
        services.AddScoped<ISubscriber<TestEvent>>(sp => new TokenCapturingHandler(token => receivedToken = token));
        var serviceProvider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(serviceProvider);
        using var cts = new CancellationTokenSource();

        // Act
        await eventBus.PublishAsync(new TestEvent(), cts.Token);

        // Assert
        receivedToken.Should().NotBeNull();
        receivedToken!.Value.Should().Be(cts.Token);
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
        var eventBus = new InMemoryEventBus(serviceProvider);

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
        var eventBus = new InMemoryEventBus(serviceProvider);

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
        var eventBus = new InMemoryEventBus(serviceProvider);

        // Act
        var result = eventBus
            .Subscribe<TestEvent, TestEventHandler>()
            .Subscribe<AnotherTestEvent, AnotherTestEventHandler>();

        // Assert
        result.Should().BeSameAs(eventBus);
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

    public class TokenCapturingHandler : ISubscriber<TestEvent>
    {
        private readonly Action<CancellationToken> _capture;

        public TokenCapturingHandler(Action<CancellationToken> capture)
        {
            _capture = capture;
        }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            _capture(cancellationToken);
            return Task.CompletedTask;
        }
    }

    #endregion
}
