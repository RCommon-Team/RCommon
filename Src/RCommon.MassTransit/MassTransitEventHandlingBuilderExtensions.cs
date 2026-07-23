using MassTransit;
using MassTransit.Configuration;
using MassTransit.DependencyInjection;
using MassTransit.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit;
using RCommon.MassTransit.Producers;
using RCommon.MassTransit.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Extension methods for configuring MassTransit event handling within the RCommon builder pipeline.
    /// </summary>
    public static class MassTransitEventHandlingBuilderExtensions
    {
        /// <summary>
        /// Adds MassTransit and its dependencies to the <paramref name="builder"/>, and allows consumers, sagas, and activities to be configured.
        /// </summary>
        /// <param name="builder">The RCommon builder to register MassTransit services against.</param>
        /// <param name="configure">Optional configuration action for <see cref="IMassTransitEventHandlingBuilder"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for further chaining.</returns>
        /// <exception cref="ConfigurationException">Thrown if MassTransit has already been registered in this container.</exception>
        private static IServiceCollection AddMassTransit<T>(this IRCommonBuilder builder, Action<IMassTransitEventHandlingBuilder>? configure = null)
            where T : class, IMassTransitEventHandlingBuilder
        {
            if (builder.Services.Any(d => d.ServiceType == typeof(IBus)))
            {
                throw new ConfigurationException(
                    "AddMassTransit() was already called and may only be called once per container. To configure additional bus instances, refer to the documentation: https://masstransit-project.com/usage/containers/multibus.html");
            }

            AddHostedService(builder.Services);
            AddInstrumentation(builder.Services);

            // Routed through GetOrAddBuilder so repeated WithEventHandling<T> calls reuse the cached sub-builder.
            var configurator = builder.GetOrAddBuilder<T>(
                () => (T)Activator.CreateInstance(typeof(T), new object[] { builder })!);
            configure?.Invoke(configurator);

            // Complete() must run on the concrete MassTransit base type to finalize bus registration.
            if (configurator is MassTransitEventHandlingBuilder mtBuilder)
            {
                mtBuilder.Complete();
            }

            return builder.Services;
        }

        /// <summary>
        /// Registers the MassTransit hosted service, health checks, and host options required for bus lifetime management.
        /// </summary>
        /// <param name="collection">The service collection to register services into.</param>
        private static void AddHostedService(IServiceCollection collection)
        {
            collection.AddOptions();
            collection.AddHealthChecks();
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HealthCheckServiceOptions>, ConfigureBusHealthCheckServiceOptions>());

            collection.AddOptions<MassTransitHostOptions>();
            collection.TryAddSingleton<IValidateOptions<MassTransitHostOptions>, ValidateMassTransitHostOptions>();
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MassTransitHostedService>());
        }

        /// <summary>
        /// Registers MassTransit instrumentation and monitoring options into the service collection.
        /// </summary>
        /// <param name="collection">The service collection to register instrumentation services into.</param>
        private static void AddInstrumentation(IServiceCollection collection)
        {
            collection.AddOptions<InstrumentationOptions>();
            collection.AddSingleton<IConfigureOptions<InstrumentationOptions>, ConfigureDefaultInstrumentationOptions>();
        }


        /// <summary>
        /// Configures MassTransit event handling with default settings.
        /// </summary>
        /// <typeparam name="T">The <see cref="IMassTransitEventHandlingBuilder"/> implementation type.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : class, IMassTransitEventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        /// <summary>
        /// Configures MassTransit event handling with custom builder actions.
        /// Registers the generic <see cref="MassTransitEventHandler{TEvent}"/> as a scoped service
        /// and wires up MassTransit via <see cref="AddMassTransit"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IMassTransitEventHandlingBuilder"/> implementation type.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <param name="actions">Configuration delegate for MassTransit event handling.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMassTransitEventHandlingBuilder> actions)
            where T : class, IMassTransitEventHandlingBuilder
        {

            // MassTransit Event Bus
            builder.Services.AddScoped(typeof(IMassTransitEventHandler<>), typeof(MassTransitEventHandler<>));
            builder.Services.AddScoped(typeof(MassTransitEventHandler<>));
            builder.AddMassTransit<T>(actions);

            return builder;
        }

        /// <summary>
        /// Declares that <typeparamref name="TEvent"/> is published via MassTransit fan-out (<c>IBus.Publish</c>).
        /// Registers <see cref="PublishWithMassTransitEventProducer"/> (idempotent), records the event→producer
        /// subscription, and returns a handle so the route can be made durable via <c>.UseOutbox("store")</c>.
        /// </summary>
        public static IEventRouteHandle Publish<TEvent>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
        {
            builder.AddProducer<PublishWithMassTransitEventProducer>();
            builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
            return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Declares that <typeparamref name="TEvent"/> is sent point-to-point via MassTransit (<c>IBus.Send</c>).
        /// Registers <see cref="SendWithMassTransitEventProducer"/> (idempotent), records the subscription, and
        /// returns a handle for <c>.UseOutbox("store")</c>.
        /// </summary>
        public static IEventRouteHandle Send<TEvent>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
        {
            builder.AddProducer<SendWithMassTransitEventProducer>();
            builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
            return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Sets a builder-level default RCommon outbox store (recipe 2a) for every outbound route on this builder
        /// that has no explicit per-event <c>.UseOutbox()</c>. Order-independent; per-event overrides win.
        /// </summary>
        public static IMassTransitEventHandlingBuilder UseRCommonOutbox(this IMassTransitEventHandlingBuilder builder, string dataStoreName)
        {
            builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName);
            return builder;
        }

        /// <summary>
        /// Registers an inbound MassTransit consumer for <typeparamref name="TEvent"/> handled by
        /// <typeparamref name="TEventHandler"/>, and records the event→producer subscription for routing.
        /// </summary>
        public static void Consume<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddTransient<ISubscriber<TEvent>, TEventHandler>();
            builder.AddConsumer<MassTransitEventHandler<TEvent>>();
            builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        /// <summary>
        /// Obsolete alias for <see cref="Consume{TEvent,TEventHandler}"/>, retained for continuity (spec AC-17).
        /// </summary>
        [System.Obsolete("Use Consume<TEvent, TEventHandler>() instead. AddSubscriber is retained as a forwarding alias (AC-17).")]
        public static void AddSubscriber<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
            => builder.Consume<TEvent, TEventHandler>();

    }
}
