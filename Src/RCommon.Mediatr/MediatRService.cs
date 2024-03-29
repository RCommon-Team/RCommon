using MediatR;
using RCommon.Mediator;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRService : IMediatorService
    {
        private readonly IMediator _mediator;

        public MediatRService(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)

        {
            Console.WriteLine("MediatRService publishing notification: {0}", notification);
            return _mediator.Publish(notification, cancellationToken);
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            return _mediator.Send(new MediatRRequest<TRequest, TResponse>(request), cancellationToken);
        }
    }
}
