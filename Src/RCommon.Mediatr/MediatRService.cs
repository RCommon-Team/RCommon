using MediatR;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
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
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            await _mediator.Publish(notification, cancellation);
        }

        public async Task<object?> Send(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            return await _mediator.Send(notification, cancellationToken);
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _mediator.CreateStream(request, cancellationToken);
        }

    }
}
