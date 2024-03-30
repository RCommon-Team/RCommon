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
            builder.Services.TryAddSingleton<IEventProducer, T>();
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder, Func<IServiceProvider, T> getProducer) 
            where T : class, IEventProducer
        {
            builder.Services.TryAddSingleton(getProducer);
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
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);
        }
    }
}
