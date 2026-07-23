using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.Subscribers;
using RCommon.MediatR.Producers;
using RCommon.MediatR.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    /// <summary>
    /// Extension methods for configuring MediatR-based event handling within the RCommon builder pipeline.
    /// </summary>
    public static class MediatREventHandlingBuilderExtensions
    {
        /// <summary>
        /// Configures MediatR event handling with default settings and no custom actions.
        /// </summary>
        /// <typeparam name="T">The <see cref="IMediatREventHandlingBuilder"/> implementation type.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : class, IMediatREventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { }, x=> { });
        }

        /// <summary>
        /// Configures MediatR event handling with custom builder actions and default MediatR assembly registration.
        /// </summary>
        /// <typeparam name="T">The <see cref="IMediatREventHandlingBuilder"/> implementation type.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <param name="actions">Configuration delegate for MediatR event handling.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMediatREventHandlingBuilder> actions)
            where T : class, IMediatREventHandlingBuilder
        {
            // MediatR
            WithEventHandling<T>(builder, actions, mediatrActions =>
            {
                mediatrActions.RegisterServicesFromAssemblies((typeof(MediatRBuilder).GetTypeInfo().Assembly));
            });

            return builder;
        }

        /// <summary>
        /// Configures MediatR event handling with both custom event handling actions and custom MediatR service configuration.
        /// Registers <see cref="IMediatorService"/>, wires up MediatR, and creates the event handling builder via reflection.
        /// </summary>
        /// <typeparam name="T">The <see cref="IMediatREventHandlingBuilder"/> implementation type.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <param name="actions">Configuration delegate for the event handling builder.</param>
        /// <param name="mediatRActions">Configuration delegate for <see cref="MediatRServiceConfiguration"/>.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMediatREventHandlingBuilder> actions,
            Action<MediatRServiceConfiguration> mediatRActions)
            where T : class, IMediatREventHandlingBuilder
        {
            builder.Services.AddScoped<IMediatorService, MediatorService>();

            // MediatR
            builder.Services.AddMediatR(mediatRActions);

            // This will wire up common event handling.
            // Routed through GetOrAddBuilder so repeated WithEventHandling<T> calls reuse the cached sub-builder.
            var eventHandlingConfig = builder.GetOrAddBuilder<T>(
                () => (T)Activator.CreateInstance(typeof(T), new object[] { builder })!);
            actions(eventHandlingConfig);

            return builder;
        }

        /// <summary>
        /// Registers an event subscriber and its corresponding MediatR notification handler for the event handling pipeline.
        /// Also registers the event-to-producer subscription for correct routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the event.</typeparam>
        /// <param name="builder">The MediatR event handling builder.</param>
        public static void AddSubscriber<TEvent, TEventHandler>(this IMediatREventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();

            // For notifications which can be handled by multiple handlers
            builder.Services.AddScoped<INotificationHandler<MediatRNotification<TEvent>>, MediatREventHandler<TEvent, MediatRNotification<TEvent>>>();

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Declares that <typeparamref name="TEvent"/> should be published via MediatR fan-out semantics,
        /// ensuring <see cref="PublishWithMediatREventProducer"/> is registered (idempotent) and
        /// recording the event-to-producer subscription in the <see cref="EventSubscriptionManager"/>.
        /// </summary>
        /// <typeparam name="TEvent">The event type to publish. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <param name="builder">The MediatR event handling builder.</param>
        /// <returns>
        /// An <see cref="IEventRouteHandle"/> that allows further configuration, such as
        /// marking the event as durable via <c>.UseOutbox("storeName")</c>.
        /// </returns>
        /// <remarks>
        /// Calling <c>Publish&lt;T&gt;()</c> alone does <em>not</em> mark the event durable —
        /// it remains transient until <see cref="IEventRouteHandle.UseOutbox"/> is chained,
        /// or a builder-level default has been set via <see cref="UseRCommonOutbox"/>.
        /// </remarks>
        public static IEventRouteHandle Publish<TEvent>(this IMediatREventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
        {
            // Ensure the MediatR publish producer is registered -- idempotent via AddProducer<T>'s
            // own already-registered check.
            builder.AddProducer<PublishWithMediatREventProducer>();

            // Register event-to-producer subscription so the router sends this event to the right producers
            builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));

            // Delegate the durability-recording logic to the shared, builder-agnostic helper
            return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
        }
    }
}
