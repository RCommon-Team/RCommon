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

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(new MediatRRequest<TRequest>(request), cancellationToken);
        } 
    }
}
