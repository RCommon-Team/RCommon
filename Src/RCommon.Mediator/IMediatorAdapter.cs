using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    /// <summary>
    /// Defines an adapter interface that abstracts the underlying mediator implementation (e.g., MediatR, Wolverine).
    /// </summary>
    /// <remarks>
    /// Concrete implementations of this interface bridge RCommon's mediator abstraction to a specific
    /// mediator library. This enables swapping mediator implementations without changing application code.
    /// </remarks>
    public interface IMediatorAdapter
    {
        /// <summary>
        /// Sends a request to a single handler with no return value.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request to send.</typeparam>
        /// <param name="request">The request object to dispatch.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request to a single handler and returns a response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request to send.</typeparam>
        /// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
        /// <param name="request">The request object to dispatch.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResponse}"/> containing the handler's response.</returns>
        Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification to publish.</typeparam>
        /// <param name="notification">The notification object to broadcast.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default);
    }
}
