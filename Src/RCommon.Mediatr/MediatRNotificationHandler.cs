using MediatR;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRNotificationHandler<TNotification, TEvent> : INotificationHandler<TNotification, TEvent>
        where TNotification : MediatRNotification<TEvent>
    {
        private readonly IAppNotificationHandler<TEvent> _handlers;

        //the IoC should inject all handlers here
        public MediatRNotificationHandler(IAppNotificationHandler<TEvent> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

       

        public Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            var tasks = _handlers.HandleAsync(notification.Notification, cancellationToken);
            //.Select(x => x.HandleAsync(notification.Notification, cancellationToken));
            return Task.WhenAll(tasks);
        }
    }
}
