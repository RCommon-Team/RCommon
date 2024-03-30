using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.MediatR.Behaviors;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public static class MediatRBuilderExtensions
    {


        public static void AddSubscriber<TEvent, TEventHandler>(this IMediatRBuilder builder)
           where TEvent : class, ISerializableEvent
           where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddTransient<ISubscriber<TEvent>, TEventHandler>();

            // For notifications which can be handled by multiple handlers
            builder.Services.AddTransient<INotificationHandler<MediatRNotification<TEvent>>, MediatREventNotificationHandler<TEvent, MediatRNotification<TEvent>>>();

            // For requests which only have one endpoint. This should only be raised if we use the IMediator.Send method
            builder.Services.AddTransient<IRequestHandler<MediatRRequest<TEvent>>, MediatREventRequestHandler<TEvent, MediatRRequest<TEvent>>>();
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

    }
}
