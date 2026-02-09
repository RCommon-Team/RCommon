using MediatR;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator.Subscribers;
using IAppRequest = RCommon.Mediator.Subscribers.IAppRequest;

namespace RCommon.MediatR.Subscribers
{
    /// <summary>
    /// MediatR request handler that bridges <see cref="MediatRRequest{T}"/> requests
    /// to RCommon's <see cref="IAppRequestHandler{T}"/> abstraction for fire-and-forget processing.
    /// Resolves the handler from the DI container and delegates request handling.
    /// </summary>
    /// <typeparam name="T">The application request type. Must implement <see cref="IAppRequest"/>.</typeparam>
    /// <typeparam name="TRequest">The MediatR request wrapper type.</typeparam>
    public class MediatRRequestHandler<T, TRequest> : IRequestHandler<TRequest>
        where T : class, IAppRequest
        where TRequest : MediatRRequest<T>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRRequestHandler{T, TRequest}"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="IAppRequestHandler{T}"/> at runtime.</param>
        public MediatRRequestHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handles the MediatR request by resolving the RCommon request handler and delegating the request.
        /// </summary>
        /// <param name="request">The MediatR request containing the wrapped application request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="NullReferenceException">Thrown when the handler cannot be resolved from the service provider.</exception>
        public async Task Handle(TRequest request, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var handler = (IAppRequestHandler<T>?)_serviceProvider.GetService(typeof(IAppRequestHandler<T>));

            Guard.Against<NullReferenceException>(handler == null,
                "IAppRequestHandler<T> of type: " + typeof(T).GetGenericTypeName() + " could not be resolved by IServiceProvider");

            // Handle the event using the event handler we resolved
            await handler!.HandleAsync(request.Request);
        }
    }

    /// <summary>
    /// MediatR request handler that bridges <see cref="MediatRRequest{T, TResponse}"/> requests
    /// to RCommon's <see cref="IAppRequestHandler{T, TResponse}"/> abstraction for request/response processing.
    /// Resolves the handler from the DI container and delegates request handling.
    /// </summary>
    /// <typeparam name="T">The application request type. Must implement <see cref="IAppRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TRequest">The MediatR request wrapper type.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public class MediatRRequestHandler<T, TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where T : class, IAppRequest<TResponse>
        where TRequest : MediatRRequest<T, TResponse>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRRequestHandler{T, TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="IAppRequestHandler{T, TResponse}"/> at runtime.</param>
        public MediatRRequestHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handles the MediatR request by resolving the RCommon request handler and delegating the request.
        /// </summary>
        /// <param name="request">The MediatR request containing the wrapped application request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response produced by the resolved application request handler.</returns>
        /// <exception cref="NullReferenceException">Thrown when the handler cannot be resolved from the service provider.</exception>
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var handler = (IAppRequestHandler<T, TResponse>?)_serviceProvider.GetService(typeof(IAppRequestHandler<T, TResponse>));

            Guard.Against<NullReferenceException>(handler == null,
                "IAppRequestHandler<T> of type: " + typeof(T).GetGenericTypeName() + " could not be resolved by IServiceProvider");

            // Handle the event using the event handler we resolved
            return await handler!.HandleAsync(request.Request);
        }
    }
}
