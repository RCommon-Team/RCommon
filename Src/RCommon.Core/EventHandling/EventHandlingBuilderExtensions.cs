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
    /// <summary>
    /// Extension methods for configuring event handling on <see cref="IRCommonBuilder"/>
    /// and registering <see cref="IEventProducer"/> instances on <see cref="IEventHandlingBuilder"/>.
    /// </summary>
    public static class EventHandlingBuilderExtensions
    {
        /// <summary>
        /// Configures event handling using the specified <typeparamref name="T"/> builder with default settings.
        /// </summary>
        /// <typeparam name="T">The <see cref="IEventHandlingBuilder"/> implementation type to use.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for method chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IEventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        /// <summary>
        /// Configures event handling using the specified <typeparamref name="T"/> builder and applies custom configuration.
        /// </summary>
        /// <typeparam name="T">The <see cref="IEventHandlingBuilder"/> implementation type to use.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">An action to configure the event handling builder.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for method chaining.</returns>
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IEventHandlingBuilder
        {
            // Event Handling Configurations
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
            actions(eventHandlingConfig);
            return builder;
        }

        /// <summary>
        /// Registers an <see cref="IEventProducer"/> of type <typeparamref name="T"/> as a singleton service
        /// and associates it with the current builder type in the <see cref="EventSubscriptionManager"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IEventProducer"/> implementation type.</typeparam>
        /// <param name="builder">The event handling builder.</param>
        public static void AddProducer<T>(this IEventHandlingBuilder builder)
            where T : class, IEventProducer
        {
            builder.Services.AddSingleton<IEventProducer, T>();

            // Track which producer type is associated with this builder type
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
        }

        /// <summary>
        /// Registers an <see cref="IEventProducer"/> of type <typeparamref name="T"/> as a singleton service
        /// using a factory delegate, and associates it with the current builder type.
        /// </summary>
        /// <typeparam name="T">The <see cref="IEventProducer"/> implementation type.</typeparam>
        /// <param name="builder">The event handling builder.</param>
        /// <param name="getProducer">A factory function to create the producer instance.</param>
        public static void AddProducer<T>(this IEventHandlingBuilder builder, Func<IServiceProvider, T> getProducer)
            where T : class, IEventProducer
        {
            builder.Services.AddSingleton(getProducer);

            // Track which producer type is associated with this builder type
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
        }

        /// <summary>
        /// Registers an existing <see cref="IEventProducer"/> instance as a singleton and associates it
        /// with the current builder type. Also registers the producer as an <see cref="IHostedService"/>
        /// if it implements that interface.
        /// </summary>
        /// <typeparam name="T">The <see cref="IEventProducer"/> implementation type.</typeparam>
        /// <param name="builder">The event handling builder.</param>
        /// <param name="producer">The producer instance to register.</param>
        public static void AddProducer<T>(this IEventHandlingBuilder builder, T producer)
            where T : class, IEventProducer
        {
            builder.Services.TryAddSingleton(producer);
            builder.Services.TryAddSingleton<IEventProducer>(sp => sp.GetRequiredService<T>());

            // Also register as IHostedService if the producer implements it
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
        /// <param name="services">The service collection to search.</param>
        /// <returns>The <see cref="EventSubscriptionManager"/> instance, or <c>null</c> if not registered.</returns>
        public static EventSubscriptionManager? GetSubscriptionManager(this IServiceCollection services)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(EventSubscriptionManager));
            return descriptor?.ImplementationInstance as EventSubscriptionManager;
        }
    }
}
