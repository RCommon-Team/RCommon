using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.ApplicationServices.Validation;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.MediatR.Behaviors;
using RCommon.Mediator.Subscribers;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace RCommon
{
    /// <summary>
    /// Extension methods for configuring notifications, requests, and pipeline behaviors
    /// on an <see cref="IMediatRBuilder"/> instance.
    /// </summary>
    public static class MediatRBuilderExtensions
    {

        /// <summary>
        /// Registers a notification subscriber and its corresponding MediatR <see cref="INotificationHandler{TNotification}"/>.
        /// Notifications are delivered to all registered handlers (fan-out).
        /// </summary>
        /// <typeparam name="T">The notification event type. Must implement <see cref="IAppNotification"/>.</typeparam>
        /// <typeparam name="TEventHandler">The subscriber implementation that handles the notification.</typeparam>
        /// <param name="builder">The MediatR builder.</param>
        public static void AddNotification<T, TEventHandler>(this IMediatRBuilder builder)
           where T : class, IAppNotification
           where TEventHandler : class, ISubscriber<T>
        {
            builder.Services.AddScoped<ISubscriber<T>, TEventHandler>();

            // For notifications which can be handled by multiple handlers
            builder.Services.AddScoped<INotificationHandler<MediatRNotification<T>>, MediatRNotificationHandler<T, MediatRNotification<T>>>();
        }

        /// <summary>
        /// Registers a request handler for a fire-and-forget request (no response).
        /// Requests are handled by a single handler via MediatR's <c>Send</c> method.
        /// </summary>
        /// <typeparam name="TRequest">The request type. Must implement <see cref="IAppRequest"/>.</typeparam>
        /// <typeparam name="TEventHandler">The handler that processes the request.</typeparam>
        /// <param name="builder">The MediatR builder.</param>
        public static void AddRequest<TRequest, TEventHandler>(this IMediatRBuilder builder)
           where TRequest : class, IAppRequest
           where TEventHandler : class, IAppRequestHandler<TRequest>
        {
            builder.Services.AddScoped<IAppRequestHandler<TRequest>, TEventHandler>();

            // For requests which only have one endpoint. This should only be raised if we use the IMediator.Send method
            builder.Services.AddScoped<IRequestHandler<MediatRRequest<TRequest>>, 
                MediatRRequestHandler<TRequest, MediatRRequest<TRequest>>>();
        }

        /// <summary>
        /// Registers a request handler for a request that returns a response.
        /// Requests are handled by a single handler via MediatR's <c>Send</c> method.
        /// </summary>
        /// <typeparam name="TRequest">The request type. Must implement <see cref="IAppRequest{TResponse}"/>.</typeparam>
        /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
        /// <typeparam name="TEventHandler">The handler that processes the request and produces the response.</typeparam>
        /// <param name="builder">The MediatR builder.</param>
        public static void AddRequest<TRequest, TResponse, TEventHandler>(this IMediatRBuilder builder)
           where TRequest : class, IAppRequest<TResponse>
           where TResponse : class
           where TEventHandler : class, IAppRequestHandler<TRequest, TResponse>
        {
            builder.Services.AddScoped<IAppRequestHandler<TRequest, TResponse>, TEventHandler>();

            // For requests which only have one endpoint. This should only be raised if we use the IMediator.Send method
            builder.Services.AddScoped<IRequestHandler<MediatRRequest<TRequest, TResponse>, TResponse>, 
                MediatRRequestHandler<TRequest, MediatRRequest<TRequest, TResponse>, TResponse>>();
        }

        /// <summary>
        /// Adds logging pipeline behaviors to the MediatR request pipeline.
        /// Registers both <see cref="LoggingRequestBehavior{TRequest, TResponse}"/> and
        /// <see cref="LoggingRequestWithResponseBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="builder">The MediatR builder.</param>
        public static void AddLoggingToRequestPipeline(this IMediatRBuilder builder)
        {
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingRequestBehavior<,>));
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingRequestWithResponseBehavior<,>));
        }

        /// <summary>
        /// Adds validation pipeline behaviors to the MediatR request pipeline.
        /// Registers both <see cref="ValidatorBehavior{TRequest, TResponse}"/> and
        /// <see cref="ValidatorBehaviorForMediatR{TRequest, TResponse}"/>, along with the <see cref="IValidationService"/>.
        /// </summary>
        /// <param name="builder">The MediatR builder.</param>
        public static void AddValidationToRequestPipeline(this IMediatRBuilder builder)
        {
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehaviorForMediatR<,>));
            builder.Services.AddScoped<IValidationService, ValidationService>();
        }

        /// <summary>
        /// Adds unit of work pipeline behaviors to the MediatR request pipeline.
        /// Registers both <see cref="UnitOfWorkRequestBehavior{TRequest, TResponse}"/> and
        /// <see cref="UnitOfWorkRequestWithResponseBehavior{TRequest, TResponse}"/> so that
        /// each request executes within a transactional unit of work.
        /// </summary>
        /// <param name="builder">The MediatR builder.</param>
        public static void AddUnitOfWorkToRequestPipeline(this IMediatRBuilder builder)
        {
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkRequestBehavior<,>));
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkRequestWithResponseBehavior<,>));
        }

    }
}
