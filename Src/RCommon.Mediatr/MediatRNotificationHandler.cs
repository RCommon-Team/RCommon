using MediatR;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRNotificationHandler<TNotification> : INotificationHandler<MediatRNotification<TNotification>>
    {
        private readonly IEnumerable<INotifierHandler<TNotification>> _handlers;

        //the IoC should inject all handlers here
        public MediatRNotificationHandler(IEnumerable<INotifierHandler<TNotification>> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public Task Handle(MediatRNotification<TNotification> notification, CancellationToken cancellationToken)
        {
            var tasks = _handlers
            .Select(x => x.HandleAsync(notification.Notification, cancellationToken));
            return Task.WhenAll(tasks);
        }
    }
}
