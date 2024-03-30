using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.MediatR;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
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
            services.AddSingleton<IMediatorAdapter, MediatRAdapter>();
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
