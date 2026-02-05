using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.Wolverine;
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

        
        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);

            // Register event-to-producer subscription so the router only sends this event to producers on this builder
            var subscriptionManager = builder.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
        }
    }
}
