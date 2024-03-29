using MediatR;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    public class MediatREventHandler<TEvent> : IMediatREventHandler<TEvent>, INotificationHandler<TEvent>
        where TEvent : class, ISerializableEvent, INotification
    {
        private readonly ISubscriber<TEvent> _subscriber;

        public MediatREventHandler(ISubscriber<TEvent> subscriber)
        {
            _subscriber = subscriber;
        }

        public async Task Handle(TEvent notification, CancellationToken cancellationToken)
        {
            await _subscriber.HandleAsync(notification);
        }
    }
}
