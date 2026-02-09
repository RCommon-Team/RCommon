using MediatR;
using Microsoft.Extensions.Logging;
using RCommon.Mediator;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    /// <summary>
    /// An Adapter for <see cref="IMediator"/>
    /// </summary>
    public class MediatRAdapter : IMediatorAdapter
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRAdapter"/>.
        /// </summary>
        /// <param name="mediator">The MediatR <see cref="IMediator"/> instance to delegate operations to.</param>
        public MediatRAdapter(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// This will wrap the original notification in <see cref="MediatRNotification{TEvent}"/> and then publish it
        /// using <see cref="IMediator"/>
        /// </summary>
        /// <typeparam name="TNotification"></typeparam>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>This should raise <see cref="MediatRNotificationHandler{T, TNotification}"/> which is the 
        /// default handler for the wrapped notification</returns>
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return _mediator.Publish(new MediatRNotification<TNotification>(notification), cancellationToken);
        }

        /// <summary>
        /// Wraps the request in a <see cref="MediatRRequest{TRequest}"/> and sends it via MediatR for single-handler processing.
        /// </summary>
        /// <typeparam name="TRequest">The type of request to send.</typeparam>
        /// <param name="request">The request payload.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(new MediatRRequest<TRequest>(request), cancellationToken);
        }

        /// <summary>
        /// Wraps the request in a <see cref="MediatRRequest{TRequest, TResponse}"/> and sends it via MediatR,
        /// returning the response from the single handler.
        /// </summary>
        /// <typeparam name="TRequest">The type of request to send.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="request">The request payload.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The response produced by the request handler.</returns>
        public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            return await _mediator.Send<TResponse>(new MediatRRequest<TRequest, TResponse>(request), cancellationToken);
        }
    }
}
