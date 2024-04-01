using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.MediatR.Behaviors;
using RCommon.Mediator.Subscribers;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public static class MediatRBuilderExtensions
    {


        public static void AddNotification<T, TEventHandler>(this IMediatRBuilder builder)
           where T : class, IAppNotification
           where TEventHandler : class, ISubscriber<T>
        {
            builder.Services.AddTransient<ISubscriber<T>, TEventHandler>();

            // For notifications which can be handled by multiple handlers
            builder.Services.AddTransient<INotificationHandler<MediatRNotification<T>>, MediatRNotificationHandler<T, MediatRNotification<T>>>();
        }

        public static void AddRequest<T, TEventHandler>(this IMediatRBuilder builder)
           where T : class, IAppRequest
           where TEventHandler : class, IAppRequestHandler<T>
        {
            builder.Services.AddTransient<IAppRequestHandler<T>, TEventHandler>();

            // For requests which only have one endpoint. This should only be raised if we use the IMediator.Send method
            builder.Services.AddTransient<IRequestHandler<MediatRRequest<T>>, MediatRRequestHandler<T, MediatRRequest<T>>>();
        }

        public static void AddLoggingToRequestPipeline(this IMediatRBuilder builder)
        {
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        }

        public static void AddValidationToRequestPipeline(this IMediatRBuilder builder)
        {
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
        }

        public static void AddUnitOfWorkToRequestPipeline(this IMediatRBuilder builder)
        {
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        }

    }
}
