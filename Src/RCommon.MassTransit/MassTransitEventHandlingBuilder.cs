using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.MassTransit.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit
{
    public class MassTransitEventHandlingBuilder : ServiceCollectionBusConfigurator, IMassTransitEventHandlingBuilder
    {

        public MassTransitEventHandlingBuilder(IRCommonBuilder builder)
            :base(builder.Services)
        {
            Services = builder.Services;
            Services.AddTransient(typeof(IMassTransitEventHandler<>), typeof(MassTransitEventHandler<>));
        }

        public IServiceCollection Services { get; }
    }
}
