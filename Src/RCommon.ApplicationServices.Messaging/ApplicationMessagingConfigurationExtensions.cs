﻿using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices.Messaging;
using RCommon.ApplicationServices.Messaging.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class ApplicationMessagingConfigurationExtensions
    {

        public static IRCommonConfiguration WithMassTransit(this IRCommonConfiguration config, Action<IBusRegistrationConfigurator> massTransitConfig)
        {
            config.Services.AddScoped<IDistributedEventBroker, DistributedEventBroker>();
            config.Services.AddMassTransit(massTransitConfig);
            return config;
        }

        public static IRCommonConfiguration AddDisributedUnitOfWorkToMediatorPipeline(this IRCommonConfiguration config)
        {
            config.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DistributedUnitOfWorkBehavior<,>));
            return config;
        }

    }
}
