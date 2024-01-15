using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit
{
    public class MassTransitEventHandlingBuilder : IEventHandlingBuilder
    {
        public MassTransitEventHandlingBuilder(IServiceCollection services)
        {
            Services = services;
            this.RegisterServices(services);
        }

        protected void RegisterServices(IServiceCollection services)
        {

        }

        public IServiceCollection Services { get; }
    }
}