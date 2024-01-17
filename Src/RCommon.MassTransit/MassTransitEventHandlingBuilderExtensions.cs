using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit;
using RCommon.MassTransit.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class MassTransitEventHandlingBuilderExtensions
    {
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IMassTransitEventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMassTransitEventHandlingBuilder> actions)
            where T : IMassTransitEventHandlingBuilder
        {
            
            // Event Bus
            builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
            
            var massTransit = actions as Action<IBusRegistrationConfigurator>;
            builder.Services.AddMassTransit(massTransit);

            // Event Handling Configurations 
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(eventHandlingConfig);
            return builder;
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
            builder.AddConsumer<MassTransitEventHandler<TEvent>>();
        }

    }
}
