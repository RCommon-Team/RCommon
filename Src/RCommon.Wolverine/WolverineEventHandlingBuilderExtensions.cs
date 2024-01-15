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
using System.Threading.Tasks;
using Wolverine;

namespace RCommon
{
    public static class WolverineEventHandlingBuilderExtensions
    {
        public static IRCommonBuilder WithWolverine(this IRCommonBuilder config, Action<WolverineOptions> options)
        {
            //config.Services.AddScoped<ISerializableEventPublisher, WolverineEventPublisher>();
            return config;
        }

        public static void AddProducer<T>(this IEventHandlingBuilder config)
            where T : class, IEventProducer
        {
            config.Services.TryAddSingleton<IEventProducer, T>();
        }

        public static void AddProducer<T>(this IEventHandlingBuilder config, Func<IServiceProvider, T> getProducer)
            where T : class, IEventProducer
        {
            config.Services.TryAddSingleton(getProducer);
        }

        public static void AddProducer<T>(this IEventHandlingBuilder config, T producer)
            where T : class, IEventProducer
        {
            config.Services.TryAddSingleton(producer);
            config.Services.TryAddSingleton<IEventProducer>(sp => sp.GetRequiredService<T>());

            if (producer is IHostedService service)
            {
                config.Services.TryAddSingleton(service);
            }
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder config)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            config.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder config, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            config.Services.TryAddScoped(getSubscriber);
        }
    }
}
