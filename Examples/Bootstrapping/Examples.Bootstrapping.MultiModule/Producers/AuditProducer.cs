using RCommon.EventHandling.Producers;
using RCommon.Models.Events;

namespace Examples.Bootstrapping.MultiModule.Producers;

/// <summary>
/// No-op <see cref="IEventProducer"/> used to demonstrate cross-module producer dedup.
/// Two modules register this same producer type; the bootstrapper merges them into a single descriptor.
/// </summary>
public class AuditProducer : IEventProducer
{
    public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : ISerializableEvent
    {
        // No-op for example purposes.
        return Task.CompletedTask;
    }
}
