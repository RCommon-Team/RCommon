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
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit;
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
        private static IServiceCollection AddMassTransit(this IRCommonBuilder builder, Action<IMassTransitEventHandlingBuilder>? configure = null)
        {
            if (builder.Services.Any(d => d.ServiceType == typeof(IBus)))
            {
                throw new ConfigurationException(
                    "AddMassTransit() was already called and may only be called once per container. To configure additional bus instances, refer to the documentation: https://masstransit-project.com/usage/containers/multibus.html");
            }

            AddHostedService(builder.Services);
            AddInstrumentation(builder.Services);
            
            var configurator = new MassTransitEventHandlingBuilder(builder);
            configure?.Invoke(configurator);
            configurator.Complete();

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
            where T : IMassTransitEventHandlingBuilder
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
            where T : IMassTransitEventHandlingBuilder
        {
            
            // MassTransit Event Bus
            builder.Services.AddScoped(typeof(IMassTransitEventHandler<>), typeof(MassTransitEventHandler<>));
            builder.Services.AddScoped(typeof(MassTransitEventHandler<>));
            builder.AddMassTransit(actions);

            return builder;
        }

        /// <summary>
        /// Registers a subscriber for a specific event type and adds the corresponding MassTransit consumer.
        /// Also registers the event-to-producer subscription for correct event routing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to. Must implement <see cref="ISerializableEvent"/>.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the event.</typeparam>
        /// <param name="builder">The MassTransit event handling builder.</param>
        public static void AddSubscriber<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddTransient<ISubscriber<TEvent>, TEventHandler>();
            builder.AddConsumer<MassTransitEventHandler<TEvent>>();

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

    }
}
