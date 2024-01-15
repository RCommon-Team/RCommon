using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class MassTransitBuilderExtensions
    {

        public static IRCommonBuilder WithMassTransit(this IRCommonBuilder config, 
            Action<IBusRegistrationConfigurator> massTransitConfig)
        {
            //config.Services.AddScoped<ISerializableEventPublisher, MassTransitEventPublisher>();
            config.Services.AddMassTransit(massTransitConfig);
            return config;
        }

    }
}
