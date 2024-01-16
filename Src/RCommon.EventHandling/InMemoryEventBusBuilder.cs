using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public class InMemoryEventBusBuilder : IEventHandlingBuilder
    {

        public InMemoryEventBusBuilder(IServiceCollection services)
        {
            Services = services;
            services.AddSingleton<IEventBus>(sp =>
            {
                this.RegisterServices(this.Services);
                return new InMemoryEventBus(sp, services);
            });
            
        }

        protected void RegisterServices(IServiceCollection services)
        {
            
        }

        public IServiceCollection Services { get; }
    }
}

