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
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);
        }
    }
}
