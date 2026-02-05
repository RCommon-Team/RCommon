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
    public static class MassTransitEventHandlingBuilderExtensions
    {
        /// <summary>
        /// Adds MassTransit and its dependencies to the <paramref name="builder" />, and allows consumers, sagas, and activities to be configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        private static IServiceCollection AddMassTransit(this IRCommonBuilder builder, Action<IMassTransitEventHandlingBuilder> configure = null)
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

        private static void AddHostedService(IServiceCollection collection)
        {
            collection.AddOptions();
            collection.AddHealthChecks();
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HealthCheckServiceOptions>, ConfigureBusHealthCheckServiceOptions>());

            collection.AddOptions<MassTransitHostOptions>();
            collection.TryAddSingleton<IValidateOptions<MassTransitHostOptions>, ValidateMassTransitHostOptions>();
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MassTransitHostedService>());
        }

        private static void AddInstrumentation(IServiceCollection collection)
        {
            collection.AddOptions<InstrumentationOptions>();
            collection.AddSingleton<IConfigureOptions<InstrumentationOptions>, ConfigureDefaultInstrumentationOptions>();
        }


        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IMassTransitEventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMassTransitEventHandlingBuilder> actions)
            where T : IMassTransitEventHandlingBuilder
        {
            
            // MassTransit Event Bus
            builder.Services.AddScoped(typeof(IMassTransitEventHandler<>), typeof(MassTransitEventHandler<>));
            builder.Services.AddScoped(typeof(MassTransitEventHandler<>));
            builder.AddMassTransit(actions);

            return builder;
        }

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
