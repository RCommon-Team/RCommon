using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Messaging;
using RCommon.Messaging.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class MassTransitConfigurationExtensions
    {

        public static IRCommonConfiguration WithMassTransit(this IRCommonConfiguration config, 
            Action<IBusRegistrationConfigurator> massTransitConfig)
        {
            config.Services.AddScoped<IDistributedEventPublisher, DistributedEventPublisher>();
            config.Services.AddMassTransit(massTransitConfig);
            return config;
        }

    }
}
