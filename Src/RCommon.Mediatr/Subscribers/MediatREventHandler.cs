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
    /// <summary>
    /// MediatR notification handler that bridges <see cref="MediatRNotification{TEvent}"/> notifications
    /// to RCommon's <see cref="ISubscriber{TEvent}"/> abstraction for serializable event handling.
    /// Resolves the subscriber from the DI container and delegates event handling.
    /// </summary>
    /// <typeparam name="TEvent">The serializable event type. Must implement <see cref="ISerializableEvent"/>.</typeparam>
    /// <typeparam name="TNotification">The MediatR notification wrapper type.</typeparam>
    /// <remarks>
    /// This differs from <see cref="MediatRNotificationHandler{T, TNotification}"/> in that it handles
    /// <see cref="ISerializableEvent"/> types used in the event handling pipeline, rather than
    /// <see cref="IAppNotification"/> types used in the mediator pipeline.
    /// </remarks>
    public class MediatREventHandler<TEvent, TNotification> : INotificationHandler<TNotification>
        where TEvent : class, ISerializableEvent
        where TNotification : MediatRNotification<TEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MediatREventHandler{TEvent, TNotification}"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="ISubscriber{TEvent}"/> at runtime.</param>
        public MediatREventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handles the MediatR notification by resolving the RCommon subscriber and delegating the event.
        /// </summary>
        /// <param name="notification">The MediatR notification containing the wrapped serializable event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="NullReferenceException">Thrown when the subscriber cannot be resolved from the service provider.</exception>
        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var subscriber = (ISubscriber<TEvent>?)_serviceProvider.GetService(typeof(ISubscriber<TEvent>));

            Guard.Against<NullReferenceException>(subscriber == null,
                "ISubscriber<TEvent> of type: " + typeof(TEvent).GetGenericTypeName() + " could not be resolved by IServiceProvider");

            // Handle the event using the event handler we resolved
            await subscriber!.HandleAsync(notification.Notification);
        }
    }
}
