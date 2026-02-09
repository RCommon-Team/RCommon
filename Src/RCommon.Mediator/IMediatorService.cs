using RCommon.EventHandling;

namespace RCommon.Mediator
{
    /// <summary>
    /// Provides a high-level mediator service for sending requests and publishing notifications.
    /// </summary>
    /// <remarks>
    /// This is the primary interface that application code should depend on for mediator operations.
    /// It delegates to <see cref="IMediatorAdapter"/> internally, decoupling consumers from the
    /// underlying mediator implementation.
    /// </remarks>
    public interface IMediatorService
    {
        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification to publish.</typeparam>
        /// <param name="notification">The notification object to broadcast to all subscribers.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default);

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
    }
}