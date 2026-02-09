using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.Subscribers;
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
            where T : IMediatREventHandlingBuilder
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
            where T : IMediatREventHandlingBuilder
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
            where T : IMediatREventHandlingBuilder
        {
            builder.Services.AddScoped<IMediatorService, MediatorService>();

            // MediatR
            builder.Services.AddMediatR(mediatRActions);

            // This will wire up common event handling
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
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
    }
}
