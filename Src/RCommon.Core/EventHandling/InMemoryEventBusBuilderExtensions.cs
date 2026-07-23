using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
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

        /// <summary>
        /// Declares that <typeparamref name="TEvent"/> should be published on the in-memory bus,
        /// ensuring <see cref="PublishWithEventBusEventProducer"/> is registered (idempotent) and
        /// recording the event-to-producer subscription in the <see cref="EventSubscriptionManager"/>.
        /// </summary>
        /// <typeparam name="TEvent">The event type to publish. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <param name="builder">The in-memory event bus builder.</param>
        /// <returns>
        /// An <see cref="IEventRouteHandle"/> that allows further configuration, such as
        /// marking the event as durable via <c>.UseOutbox("storeName")</c>.
        /// </returns>
        /// <remarks>
        /// Calling <c>Publish&lt;T&gt;()</c> alone does <em>not</em> mark the event durable —
        /// it remains transient until <see cref="IEventRouteHandle.UseOutbox"/> is chained,
        /// or a builder-level default has been set via <see cref="UseRCommonOutbox"/>.
        /// </remarks>
        public static IEventRouteHandle Publish<TEvent>(this IInMemoryEventBusBuilder builder)
            where TEvent : class, ISerializableEvent
        {
            // Ensure the standard in-memory producer is registered -- idempotent via AddProducer<T>'s
            // own already-registered check, matching what AddSubscriber does.
            builder.AddProducer<PublishWithEventBusEventProducer>();

            // Register event-to-producer subscription so the router sends this event to the right producers
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));

            // Delegate the durability-recording logic to the shared, builder-agnostic helper
            return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Sets a builder-level default outbox store for all events published on this builder
        /// that do not have an explicit per-event <c>.UseOutbox()</c> override.
        /// </summary>
        /// <param name="builder">The in-memory event bus builder.</param>
        /// <param name="dataStoreName">
        /// The default outbox datastore name. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        /// <returns>The builder, for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// Order-independent: events already published before this call are retroactively marked
        /// durable (unless they were given an explicit <c>.UseOutbox()</c> override). Events
        /// published after this call inherit the default automatically.
        /// </para>
        /// <para>
        /// Precedence: per-event <c>.UseOutbox("x")</c> always beats the builder default, whether
        /// the per-event call comes before or after <c>UseRCommonOutbox</c>.
        /// </para>
        /// </remarks>
        public static IInMemoryEventBusBuilder UseRCommonOutbox(this IInMemoryEventBusBuilder builder, string dataStoreName)
        {
            // Delegate the builder-default recording logic to the shared, builder-agnostic helper
            builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName);
            return builder;
        }
    }
}
