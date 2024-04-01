using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    public class MediatorService : IMediatorService
    {
        private readonly IMediatorAdapter _mediatorAdapter;

        public MediatorService(IMediatorAdapter mediatorAdapter)
        {
            _mediatorAdapter = mediatorAdapter;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return _mediatorAdapter.Publish(notification, cancellationToken);
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            await _mediatorAdapter.Send(request, cancellationToken);
        }

    }
}
