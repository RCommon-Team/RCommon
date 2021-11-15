using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public static class ApplicationServicesMessagingConfiguration
    {

        public static ICommonApplicationServicesConfiguration WithMassTransit(this ICommonApplicationServicesConfiguration config, Action<IServiceCollectionBusConfigurator> massTransitConfig)
        {
            config.ContainerAdapter.Services.AddMassTransit(massTransitConfig);
            return config;
        }
    }
}
