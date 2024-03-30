using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    public static class MediatREventHandlingBuilderExtensions
    {
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IMediatREventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { }, x=> { });
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMediatREventHandlingBuilder> actions)
            where T : IMediatREventHandlingBuilder
        {

            // MediatR
            WithEventHandling<T>(builder, actions, mediatrActions =>
            {
                mediatrActions.RegisterServicesFromAssemblies((typeof(MediatRBuilder).GetTypeInfo().Assembly));
            });

            return builder;
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<IMediatREventHandlingBuilder> actions, 
            Action<MediatRServiceConfiguration> mediatRActions)
            where T : IMediatREventHandlingBuilder
        {

            // MediatR
            //builder.Services.AddTransient(typeof(IMediatRNotificationHandler<>), typeof(MediatRNotificationHandler<>));
            //builder.Services.AddTransient(typeof(MediatREventHandler<>));
            builder.Services.AddMediatR(mediatRActions);

            // This will wire up common event handling
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(eventHandlingConfig);

            return builder;
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IMediatREventHandlingBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddTransient<ISubscriber<TEvent>, TEventHandler>();

            // For notifications which can be handled by multiple handlers
            builder.Services.AddTransient<INotificationHandler<MediatRNotification<TEvent>>, MediatRNotificationHandler<TEvent, MediatRNotification<TEvent>>>();

            // For requests which only have one endpoint. This should only be raised if we use the IMediator.Send method
            builder.Services.AddTransient<IRequestHandler<MediatRRequest<TEvent>>, MediatRRequestHandler<TEvent, MediatRRequest<TEvent>>>();
        }
    }
}
