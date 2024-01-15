using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.MediatR.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public static class MediatRBuilderExtensions
    {


        public static IRCommonBuilder AddMediatR(this IRCommonBuilder config, Action<MediatRServiceConfiguration> mediatrOptions )
        {
            config.Services.AddMediatR(mediatrOptions);
            return config;
        }

        public static IRCommonBuilder AddLoggingToMediatorPipeline(this IRCommonBuilder config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            return config;
        }

        public static IRCommonBuilder AddValidationToMediatorPipeline(this IRCommonBuilder config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
            return config;
        }

        public static IRCommonBuilder AddUnitOfWorkToMediatorPipeline(this IRCommonBuilder config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
            return config;
        }

        public static IRCommonBuilder AddDisributedUnitOfWorkToMediatorPipeline(this IRCommonBuilder config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DistributedUnitOfWorkBehavior<,>));
            return config;
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder config)
            where TEventHandler : class, IAppNotificationHandler<TEvent>
            where TEvent : IAppNotification
        {
            //config.Services.AddTransient(typeof(IAppNotificationHandler<>), typeof(TEventHandler<>));
            //config.Services.AddTransient<IAppNotificationHandler<TEvent>, TEventHandler>();
            config.Services.AddTransient<IAppNotificationHandler<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IEventHandlingBuilder config, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : MediatRNotification<TEvent>
            where TEventHandler : class, IAppNotificationHandler<TEvent>
        {
            config.Services.TryAddTransient(getSubscriber);
        }

    }
}
