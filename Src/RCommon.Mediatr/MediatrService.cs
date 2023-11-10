using MediatR;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediatr
{
    public class MediatrService : IMediatorService
    {
        private readonly IMediator _mediator;

        public MediatrService(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task Publish(object notification, CancellationToken cancellation = default)
        {
            await _mediator.Publish(notification, cancellation);
        }

        public async Task Send(object notification, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(notification, cancellationToken);
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            return _mediator.CreateStream(request, cancellationToken);
        }

    }
}
