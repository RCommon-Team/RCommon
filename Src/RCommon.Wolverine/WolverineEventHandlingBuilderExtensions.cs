using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.Wolverine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon
{
    /// <summary>
    /// Extension methods for configuring Wolverine event handling within the RCommon builder pipeline.
    /// </summary>
    public static class WolverineEventHandlingBuilderExtensions
    {

        /// <summary>
        /// Registers a subscriber for a specific event type and records the event-to-producer subscription for routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the event.</typeparam>
        /// <param name="builder">The Wolverine event handling builder.</param>
        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Registers a subscriber for a specific event type using a factory delegate and records
        /// the event-to-producer subscription for routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the event.</typeparam>
        /// <param name="builder">The Wolverine event handling builder.</param>
        /// <param name="getSubscriber">Factory delegate to create the subscriber from the service provider.</param>
        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }
    }
}
