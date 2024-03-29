using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    public static class MediatREventHandlingBuilderExtensions
    {
        public static void AddSubscriber<TEvent, TEventHandler>(this IMediatREventHandlingBuilder config)
            where TEventHandler : class, IAppNotificationHandler<TEvent>
            where TEvent : IAppNotification
        {
            //config.Services.AddTransient(typeof(IAppNotificationHandler<>), typeof(TEventHandler<>));
            //config.Services.AddTransient<IAppNotificationHandler<TEvent>, TEventHandler>();
            config.Services.AddTransient<IAppNotificationHandler<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IMediatREventHandlingBuilder config, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEventHandler : class, IAppNotificationHandler<TEvent>
            where TEvent : IAppNotification
        {
            config.Services.TryAddTransient(getSubscriber);
        }




        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IMediatREventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMediatREventHandlingBuilder> actions)
            where T : IMediatREventHandlingBuilder
        {

            // MassTransit Event Bus
            builder.Services.AddTransient(typeof(IMassTransitEventHandler<>), typeof(MassTransitEventHandler<>));
            builder.Services.AddTransient(typeof(MassTransitEventHandler<>));
            builder.AddMassTransit(actions);

            return builder;
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddTransient<ISubscriber<TEvent>, TEventHandler>();
            builder.AddConsumer<MassTransitEventHandler<TEvent>>();
        }
    }
}
