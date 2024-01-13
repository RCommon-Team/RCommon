using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public static class EventHandlingConfigurationExtensions
    {
        public static IRCommonConfiguration WithEventHandling<T>(this IRCommonConfiguration config)
            where T : IEventHandlingConfiguration
        {
            return WithEventHandling<T>(config, x => { });
        }

        public static IRCommonConfiguration WithEventHandling<T>(this IRCommonConfiguration config, Action<T> actions)
            where T : IEventHandlingConfiguration
        {

            // Event Producer Store Management
            StaticEventProducerStore.EventProducers = (StaticEventProducerStore.EventProducers == null ? new System.Collections.Concurrent.ConcurrentDictionary<Type, Type>() : StaticEventProducerStore.EventProducers);
            config.Services.AddSingleton<IEventProducerRegistry, StaticEventProducerRegistry>();

            // Event Handling Configurations 
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { config.Services });
            actions(eventHandlingConfig);
            return config;
        }

        public static void AddProducer<T>(this IEventHandlingConfiguration config) 
            where T : class, IEventProducer
        {
            config.Services.TryAddSingleton<IEventProducer, T>();
        }

        public static void AddProducer<T>(this IEventHandlingConfiguration config, Func<IServiceProvider, T> getProducer) 
            where T : class, IEventProducer
        {
            config.Services.TryAddSingleton(getProducer);
        }

        public static void AddProducer<T>(this IEventHandlingConfiguration config, T producer) 
            where T : class, IEventProducer
        {
            config.Services.TryAddSingleton(producer);
            config.Services.TryAddSingleton<IEventProducer>(sp => sp.GetRequiredService<T>());

            if (producer is IHostedService service)
            {
                config.Services.TryAddSingleton(service);
            }
        }
    }
}
