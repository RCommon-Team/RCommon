using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRNotificationHandler<TMediatRNotification, TNotifier>
        where TMediatRNotification : MediatRNotification<TNotifier>
        where TNotifier : INotifier
    {
        private readonly IEnumerable<INotifierHandler<TNotifier>> _handlers;

        //the IoC should inject all domain handlers here
        public MediatRNotificationHandler(IEnumerable<INotifierHandler<TNotifier>> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public Task Handle(TMediatRNotification notification, CancellationToken cancellationToken)
        {
            var handlingTasks = _handlers.Select(h => h.HandleAsync(notification.Notification, cancellationToken));
            return Task.WhenAll(handlingTasks);
        }
    }
}
