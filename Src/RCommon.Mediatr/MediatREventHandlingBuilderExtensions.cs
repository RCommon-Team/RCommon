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
        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder config)
            where TEventHandler : class, IAppNotificationHandler<TEvent>
            where TEvent : IAppNotification
        {
            //config.Services.AddTransient(typeof(IAppNotificationHandler<>), typeof(TEventHandler<>));
            //config.Services.AddTransient<IAppNotificationHandler<TEvent>, TEventHandler>();
            config.Services.AddTransient<IAppNotificationHandler<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder config, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEventHandler : class, IAppNotificationHandler<TEvent>
            where TEvent : IAppNotification
        {
            config.Services.TryAddTransient(getSubscriber);
        }
    }
}
