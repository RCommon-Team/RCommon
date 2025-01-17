using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public static class InMemoryEventBusBuilderExtensions
    {
        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);
        }
    }
}
