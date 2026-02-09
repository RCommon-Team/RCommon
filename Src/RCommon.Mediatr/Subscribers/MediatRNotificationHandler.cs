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
    /// <summary>
    /// MediatR notification handler that bridges <see cref="MediatRNotification{T}"/> notifications
    /// to RCommon's <see cref="ISubscriber{T}"/> abstraction for application notifications.
    /// Resolves the subscriber from the DI container and delegates event handling.
    /// </summary>
    /// <typeparam name="T">The application notification type. Must implement <see cref="IAppNotification"/>.</typeparam>
    /// <typeparam name="TNotification">The MediatR notification wrapper type.</typeparam>
    public class MediatRNotificationHandler<T, TNotification> : INotificationHandler<TNotification>
        where T : class, IAppNotification
        where TNotification : MediatRNotification<T>
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRNotificationHandler{T, TNotification}"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="ISubscriber{T}"/> at runtime.</param>
        public MediatRNotificationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handles the MediatR notification by resolving the RCommon subscriber and delegating the event.
        /// </summary>
        /// <param name="notification">The MediatR notification containing the wrapped event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="NullReferenceException">Thrown when the subscriber cannot be resolved from the service provider.</exception>
        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var subscriber = (ISubscriber<T>?) _serviceProvider.GetService(typeof(ISubscriber<T>));

            Guard.Against<NullReferenceException>(subscriber == null,
                "ISubscriber<TEvent> of type: " + typeof(T).GetGenericTypeName() + " could not be resolved by IServiceProvider");

            // Handle the event using the event handler we resolved
            await subscriber!.HandleAsync(notification.Notification);
        }
    }
}
