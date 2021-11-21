using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MediatR;
using RCommon.ApplicationServices.Messaging.Behaviors;
using RCommon.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public static class ApplicationMessagingConfigurationExtensions
    {

        public static IRCommonConfiguration WithMassTransit(this IRCommonConfiguration config, Action<IServiceCollectionBusConfigurator> massTransitConfig)
        {
            config.ContainerAdapter.AddScoped<IDistributedEventBroker, DistributedEventBroker>();
            config.ContainerAdapter.Services.AddMassTransit(massTransitConfig);
            return config;
        }

        public static IRCommonConfiguration WithMediatRDistributedUnitOfWorkBehavior(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(DistributedUnitOfWorkBehavior<,>), typeof(IPipelineBehavior<,>));
            return config;
        }

    }
}
