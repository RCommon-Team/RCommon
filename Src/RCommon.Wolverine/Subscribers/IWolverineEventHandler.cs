using Wolverine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Models.Events;

namespace RCommon.MassTransit.Subscribers
{
    /// <summary>
    /// Non-generic marker interface for Wolverine event handlers within the RCommon framework.
    /// </summary>
    public interface IWolverineEventHandler
    {
    }

    /// <summary>
    /// Generic interface for Wolverine event handlers that process a specific distributed event type.
    /// </summary>
    /// <typeparam name="TDistributedEvent">The distributed event type to handle. Must implement <see cref="ISerializableEvent"/>.</typeparam>
    public interface IWolverineEventHandler<in TDistributedEvent> : IWolverineEventHandler
        where TDistributedEvent : class, ISerializableEvent
    {
        /// <summary>
        /// Handles the distributed event asynchronously.
        /// </summary>
        /// <param name="distributedEvent">The event to handle.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TDistributedEvent distributedEvent, CancellationToken cancellationToken = default);
    }
}
