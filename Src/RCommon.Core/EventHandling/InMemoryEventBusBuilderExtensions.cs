using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    /// <summary>
    /// Extension methods for <see cref="IInMemoryEventBusBuilder"/> to register event subscribers
    /// and associate them with the correct event producers via the <see cref="EventSubscriptionManager"/>.
    /// </summary>
    public static class InMemoryEventBusBuilderExtensions
    {
        /// <summary>
        /// Registers a scoped subscriber for the specified event type and records the event-to-producer
        /// subscription so the <see cref="IEventRouter"/> routes this event only to the correct producers.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber type that handles <typeparamref name="TEvent"/>.</typeparam>
        /// <param name="builder">The in-memory event bus builder.</param>
        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();

            // A subscriber with no registered IEventProducer for this builder is never invoked, with no
            // error of any kind. There is no scenario where a consumer wants a subscriber wired but not
            // routed, so ensure the standard in-memory producer is registered -- idempotent via
            // AddProducer<T>'s own already-registered check, so calling this repeatedly across multiple
            // AddSubscriber calls is a no-op after the first.
            builder.AddProducer<PublishWithEventBusEventProducer>();

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Registers a scoped subscriber for the specified event type using a factory delegate,
        /// and records the event-to-producer subscription for correct event routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber type that handles <typeparamref name="TEvent"/>.</typeparam>
        /// <param name="builder">The in-memory event bus builder.</param>
        /// <param name="getSubscriber">A factory function to create the subscriber instance.</param>
        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);

            // See the parameterless AddSubscriber overload for why this is required.
            builder.AddProducer<PublishWithEventBusEventProducer>();

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }
    }
}
