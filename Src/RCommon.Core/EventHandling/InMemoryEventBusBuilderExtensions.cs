using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling.Producers;
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

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }
    }
}
