using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using RCommon.Wolverine;
using RCommon.Wolverine.Producers;
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
        /// Declares that <typeparamref name="TEvent"/> is published via Wolverine fan-out
        /// (<c>IMessageBus.PublishAsync</c>). Registers <see cref="PublishWithWolverineEventProducer"/>
        /// (idempotent), records the event→producer subscription, and returns a handle so the route can
        /// be made durable via <c>.UseOutbox("store")</c>.
        /// </summary>
        public static IEventRouteHandle Publish<TEvent>(this IWolverineEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
        {
            builder.AddProducer<PublishWithWolverineEventProducer>();
            builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
            return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Declares that <typeparamref name="TEvent"/> is sent point-to-point via Wolverine
        /// (<c>IMessageBus.SendAsync</c>). Registers <see cref="SendWithWolverineEventProducer"/>
        /// (idempotent), records the subscription, and returns a handle for <c>.UseOutbox("store")</c>.
        /// </summary>
        public static IEventRouteHandle Send<TEvent>(this IWolverineEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
        {
            builder.AddProducer<SendWithWolverineEventProducer>();
            builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
            return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Sets a builder-level default RCommon outbox store (recipe 2a) for every outbound route on this
        /// builder that has no explicit per-event <c>.UseOutbox()</c>. Order-independent; per-event overrides win.
        /// </summary>
        public static IWolverineEventHandlingBuilder UseRCommonOutbox(this IWolverineEventHandlingBuilder builder, string dataStoreName)
        {
            builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName);
            return builder;
        }

        /// <summary>
        /// Registers an inbound Wolverine consumer for <typeparamref name="TEvent"/> handled by
        /// <typeparamref name="TEventHandler"/>, and records the event subscription for routing.
        /// </summary>
        public static void Consume<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();

            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Registers an inbound Wolverine consumer for <typeparamref name="TEvent"/> using a factory
        /// delegate, and records the event subscription for routing.
        /// </summary>
        public static void Consume<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);

            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Registers a subscriber for a specific event type and records the event-to-producer subscription for routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the event.</typeparam>
        /// <param name="builder">The Wolverine event handling builder.</param>
        [System.Obsolete("Use Consume<TEvent, TEventHandler>() instead. AddSubscriber is retained as a forwarding alias (AC-17).")]
        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
            => builder.Consume<TEvent, TEventHandler>();

        /// <summary>
        /// Registers a subscriber for a specific event type using a factory delegate and records
        /// the event-to-producer subscription for routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the event.</typeparam>
        /// <param name="builder">The Wolverine event handling builder.</param>
        /// <param name="getSubscriber">Factory delegate to create the subscriber from the service provider.</param>
        [System.Obsolete("Use Consume<TEvent, TEventHandler>(Func<IServiceProvider, TEventHandler>) instead. Retained as a forwarding alias (AC-17).")]
        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
            => builder.Consume<TEvent, TEventHandler>(getSubscriber);
    }
}
