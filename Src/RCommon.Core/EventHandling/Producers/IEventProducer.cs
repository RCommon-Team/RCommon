using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Defines a producer responsible for dispatching serializable events to their destination
    /// (e.g., in-memory bus, message broker, or external system).
    /// </summary>
    /// <seealso cref="PublishWithEventBusEventProducer"/>
    public interface IEventProducer
    {
        /// <summary>
        /// Produces (dispatches) the specified event asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to produce. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <param name="event">The event instance to produce.</param>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous produce operation.</returns>
        Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent;
    }
}
