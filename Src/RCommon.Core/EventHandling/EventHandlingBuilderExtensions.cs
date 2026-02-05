using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon
{
    public static class EventHandlingBuilderExtensions
    {
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IEventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IEventHandlingBuilder
        {
            // Event Handling Configurations
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(eventHandlingConfig);
            return builder;
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder)
            where T : class, IEventProducer
        {
            builder.Services.AddSingleton<IEventProducer, T>();

            // Track which producer type is associated with this builder type
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder, Func<IServiceProvider, T> getProducer)
            where T : class, IEventProducer
        {
            builder.Services.AddSingleton(getProducer);

            // Track which producer type is associated with this builder type
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder, T producer)
            where T : class, IEventProducer
        {
            builder.Services.TryAddSingleton(producer);
            builder.Services.TryAddSingleton<IEventProducer>(sp => sp.GetRequiredService<T>());

            if (producer is IHostedService service)
            {
                builder.Services.TryAddSingleton(service);
            }

            // Track which producer type is associated with this builder type
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
        }

        /// <summary>
        /// Retrieves the <see cref="EventSubscriptionManager"/> singleton instance from the service collection
        /// during configuration time (before the service provider is built).
        /// </summary>
        public static EventSubscriptionManager? GetSubscriptionManager(this IServiceCollection services)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(EventSubscriptionManager));
            return descriptor?.ImplementationInstance as EventSubscriptionManager;
        }
    }
}
