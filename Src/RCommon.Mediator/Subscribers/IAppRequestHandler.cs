using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Subscribers
{
    /// <summary>
    /// Defines a handler for a request that does not return a value.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to handle.</typeparam>
    /// <remarks>
    /// Implement this interface to handle requests of type <typeparamref name="TRequest"/> dispatched
    /// via <see cref="IMediatorService.Send{TRequest}"/>. Each request type should have exactly one handler.
    /// </remarks>
    public interface IAppRequestHandler<TRequest>
    {
        /// <summary>
        /// Handles the specified request asynchronously.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines a handler for a request that returns a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to handle.</typeparam>
    /// <typeparam name="TResponse">The type of the response to return.</typeparam>
    /// <remarks>
    /// Implement this interface to handle requests of type <typeparamref name="TRequest"/> dispatched
    /// via <see cref="IMediatorService.Send{TRequest, TResponse}"/>. Each request type should have exactly one handler.
    /// </remarks>
    public interface IAppRequestHandler<TRequest, TResponse>
    {
        /// <summary>
        /// Handles the specified request asynchronously and returns a response.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResponse}"/> containing the handler's response.</returns>
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
