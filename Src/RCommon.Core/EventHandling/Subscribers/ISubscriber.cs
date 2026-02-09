
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{

    /// <summary>
    /// Defines a strongly-typed event subscriber that handles events of type <typeparamref name="TEvent"/>.
    /// Implementations are resolved from the DI container when events are published via the <see cref="IEventBus"/>.
    /// </summary>
    /// <typeparam name="TEvent">The type of event this subscriber handles.</typeparam>
    public interface ISubscriber<TEvent>
    {
        /// <summary>
        /// Handles the specified event asynchronously.
        /// </summary>
        /// <param name="event">The event instance to handle.</param>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous handling operation.</returns>
        public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
