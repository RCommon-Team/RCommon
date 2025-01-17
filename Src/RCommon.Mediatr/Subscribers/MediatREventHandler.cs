using MediatR;
using RCommon.Models.Events;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator;

namespace RCommon.MediatR.Subscribers
{
    public class MediatREventHandler<TEvent, TNotification> : INotificationHandler<TNotification>
        where TEvent : class, ISerializableEvent
        where TNotification : MediatRNotification<TEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        public MediatREventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var subscriber = (ISubscriber<TEvent>)_serviceProvider.GetService(typeof(ISubscriber<TEvent>));

            Guard.Against<NullReferenceException>(subscriber == null,
                "ISubscriber<TEvent> of type: " + typeof(TEvent).GetGenericTypeName() + " could not be resolved by IServiceProvider");

            // Handle the event using the event handler we resolved
            await subscriber.HandleAsync(notification.Notification);
        }
    }
}
