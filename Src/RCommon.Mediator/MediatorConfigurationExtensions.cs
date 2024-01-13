using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class MediatorConfigurationExtensions
    {
        public static void AddEvent<T>(this IEventHandlingConfiguration config)
            where T : class, ISerializableEvent, INotifier
        {
            config.Services.TryAddTransient<T>();
        }

        public static void AddEvent<T>(this IEventHandlingConfiguration config, Func<IServiceProvider, T> getEvent)
            where T : class, ISerializableEvent, INotifier
        {
            config.Services.TryAddTransient(getEvent);
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingConfiguration config)
            where TEvent : class, ISerializableEvent, INotifier
            where TEventHandler : class, INotifierHandler<TEvent>
        {
            config.Services.AddTransient<INotifierHandler<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingConfiguration config, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class, ISerializableEvent, INotifier
            where TEventHandler : class, INotifierHandler<TEvent>
        {
            config.Services.TryAddTransient(getSubscriber);
        }
    }
}
