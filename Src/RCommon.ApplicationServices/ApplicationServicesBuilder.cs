using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public class ApplicationServicesBuilder : IEventHandlingBuilder
    {
        public ApplicationServicesBuilder(IServiceCollection services)
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