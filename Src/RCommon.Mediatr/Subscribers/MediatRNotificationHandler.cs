using MediatR;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator;
using RCommon.Mediator.Subscribers;

namespace RCommon.MediatR.Subscribers
{
    public class MediatRNotificationHandler<T, TNotification> : INotificationHandler<TNotification>
        where T : class, IAppNotification
        where TNotification : MediatRNotification<T>
    {
        private readonly IServiceProvider _serviceProvider;

        public MediatRNotificationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var subscriber = (ISubscriber<T>) _serviceProvider.GetService(typeof(ISubscriber<T>));
            
            Guard.Against<NullReferenceException>(subscriber == null, 
                "ISubscriber<TEvent> of type: " + typeof(T).GetGenericTypeName() + " could not be resolved by IServiceProvider");
            
            // Handle the event using the event handler we resolved
            await subscriber.HandleAsync(notification.Notification);
        }
    }
}
