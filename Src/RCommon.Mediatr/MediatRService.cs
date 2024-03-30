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
            return _mediator.Publish(notification, cancellationToken);
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(request, cancellationToken);
        }
    }
}
