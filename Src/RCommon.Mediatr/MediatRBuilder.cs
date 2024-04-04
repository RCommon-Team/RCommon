using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.MediatR;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRBuilder : IMediatRBuilder
    {

        public MediatRBuilder(IRCommonBuilder builder)
        {


            this.RegisterServices(builder.Services);
            Services = builder.Services;

        }

        protected void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IMediatorAdapter, MediatRAdapter>();

            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssemblies((typeof(MediatRBuilder).GetTypeInfo().Assembly));
            });
        }

        public IMediatRBuilder Configure(Action<MediatRServiceConfiguration> options)
        {
            Services.AddMediatR(options);
            return this;
        }

        public IMediatRBuilder Configure(MediatRServiceConfiguration options)
        {
            Services.AddMediatR(options);
            return this;
        }

        public IServiceCollection Services { get; }
    }
}
