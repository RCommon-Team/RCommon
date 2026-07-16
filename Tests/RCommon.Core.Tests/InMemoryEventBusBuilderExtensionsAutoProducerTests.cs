using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

/// <summary>
/// Locks in the fix from docs/specs/event-handling/producer-auto-registration.md: calling
/// AddSubscriber alone (no explicit AddProducer call) is sufficient for the subscriber to actually
/// be invoked, because AddSubscriber now auto-registers PublishWithEventBusEventProducer.
/// </summary>
public class InMemoryEventBusBuilderExtensionsAutoProducerTests
{
    [Fact]
    public async Task AddSubscriber_WithoutExplicitAddProducer_SubscriberHandleAsyncFires()
    {
        // Arrange -- the core regression test for the originally reported bug: AddSubscriber alone,
        // with no separate AddProducer<PublishWithEventBusEventProducer>() call anywhere.
        AutoProducerTestEventHandler.HandledEvents.Clear();
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
        {
            eventHandling.AddSubscriber<AutoProducerTestEvent, AutoProducerTestEventHandler>();
        });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<IEventRouter>();
        var testEvent = new AutoProducerTestEvent();

        // Act
        await router.RouteEventsAsync(new List<ISerializableEvent> { testEvent });

        // Assert
        AutoProducerTestEventHandler.HandledEvents.Should().Contain(testEvent);
    }

    [Fact]
    public void AddSubscriber_MultipleEventTypesOnSameBuilder_RegistersProducerExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
        {
            eventHandling.AddSubscriber<AutoProducerTestEvent, AutoProducerTestEventHandler>();
            eventHandling.AddSubscriber<SecondAutoProducerTestEvent, SecondAutoProducerTestEventHandler>();
        });

        // Assert -- exactly one IEventProducer -> PublishWithEventBusEventProducer descriptor,
        // even though AddSubscriber was called twice.
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithEventBusEventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    public class AutoProducerTestEvent : ISyncEvent
    {
    }

    public class SecondAutoProducerTestEvent : ISyncEvent
    {
    }

    public class AutoProducerTestEventHandler : ISubscriber<AutoProducerTestEvent>
    {
        public static readonly List<AutoProducerTestEvent> HandledEvents = new();

        public Task HandleAsync(AutoProducerTestEvent @event, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    public class SecondAutoProducerTestEventHandler : ISubscriber<SecondAutoProducerTestEvent>
    {
        public Task HandleAsync(SecondAutoProducerTestEvent @event, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
