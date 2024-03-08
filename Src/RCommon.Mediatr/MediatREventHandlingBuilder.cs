using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    public class MediatREventHandlingBuilder : IEventHandlingBuilder
    {
        public MediatREventHandlingBuilder(IServiceCollection services)
        {
            Services = services;
            this.RegisterServices(services);
        }

        protected void RegisterServices(IServiceCollection services)
        {
            services.AddMediatR(mediatr =>
            {
                mediatr.RegisterServicesFromAssemblyContaining<MediatREventHandlingBuilder>();
            });

            
            services.AddSingleton<IMediatorService, MediatrService>();
            //services.AddTransient(typeof(INotificationHandler<>), typeof(INotificationHandler<,>));
            services.AddTransient(typeof(INotificationHandler<,>), typeof(MediatRNotificationHandler<,>));
            services.AddTransient(typeof(IRequestHandler<,>), typeof(MediatRRequestHandler<,>));
        }

        public IServiceCollection Services { get; }
    }
}
