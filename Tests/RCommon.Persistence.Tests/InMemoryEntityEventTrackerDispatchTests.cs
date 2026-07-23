using FluentAssertions;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Persistence.Tests;

public class InMemoryEntityEventTrackerDispatchTests
{
    public record DispatchTestEvent(string Data) : ISerializableEvent;

    /// <summary>
    /// Entity that seeds a single local event at construction. Used so the tracker has something to seed.
    /// </summary>
    private class DispatchTestEntity : BusinessEntity<int>
    {
        public DispatchTestEntity(ISerializableEvent localEvent)
        {
            AddLocalEvent(localEvent);
        }
    }

    /// <summary>
    /// Recording fake router. Captures every event passed to <see cref="AddTransactionalEvent"/> and records
    /// whether the no-arg <see cref="RouteEventsAsync(CancellationToken)"/> drain was invoked. Optionally runs a
    /// callback INSIDE the drain so tests can simulate a handler raising a new event mid-drain while the tracker's
    /// subscription is still attached.
    /// </summary>
    private sealed class RecordingEventRouter : IEventRouter
    {
        public List<ISerializableEvent> Enqueued { get; } = new();
        public bool DrainInvoked { get; private set; }
        public Func<Task>? OnDrain { get; set; }

        public void AddTransactionalEvent(ISerializableEvent serializableEvent) => Enqueued.Add(serializableEvent);

        public void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents)
            => Enqueued.AddRange(serializableEvents);

        public async Task RouteEventsAsync(CancellationToken cancellationToken = default)
        {
            DrainInvoked = true;
            if (OnDrain is not null)
            {
                await OnDrain().ConfigureAwait(false);
            }
        }

        public Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task DispatchDomainEventsAsync_Seeds_Tracked_Local_Events_And_Invokes_Drain()
    {
        var router = new RecordingEventRouter();
        var tracker = new InMemoryEntityEventTracker(router);
        var localEvent = new DispatchTestEvent("seed");
        tracker.AddEntity(new DispatchTestEntity(localEvent));

        await tracker.DispatchDomainEventsAsync();

        router.Enqueued.Should().Contain(localEvent);
        router.DrainInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchDomainEventsAsync_Captures_Events_Raised_By_A_Handler_During_The_Drain()
    {
        var router = new RecordingEventRouter();
        var tracker = new InMemoryEntityEventTracker(router);
        var seedEvent = new DispatchTestEvent("seed");
        var nextEvent = new DispatchTestEvent("next");
        var entity = new DispatchTestEntity(seedEvent);
        tracker.AddEntity(entity);

        // Simulate a handler mutating the entity DURING the drain: while RouteEventsAsync is executing the
        // tracker's subscription is attached, so AddLocalEvent flows into the router via TransactionalEventAdded.
        router.OnDrain = () =>
        {
            entity.AddLocalEvent(nextEvent);
            return Task.CompletedTask;
        };

        await tracker.DispatchDomainEventsAsync();

        router.Enqueued.Should().Contain(seedEvent);
        router.Enqueued.Should().Contain(nextEvent);
    }
}
