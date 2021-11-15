using MediatR;
using RCommon.ApplicationServices.MediatR.Behaviors;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.MediatR
{
    public static class ApplicationServicesMediatRConfiguration
    {

        public static ICommonApplicationServicesConfiguration WithMediatRLoggingBehavior(this ICommonApplicationServicesConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(LoggingBehavior<,>), typeof(IPipelineBehavior<,>));
            return config;
        }

        public static ICommonApplicationServicesConfiguration WithMediatRValidationBehavior(this ICommonApplicationServicesConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(ValidatorBehavior<,,>), typeof(IPipelineBehavior<,>));
            return config;
        }

        public static ICommonApplicationServicesConfiguration WithMediatRUnitOfWorkBehavior(this ICommonApplicationServicesConfiguration config)
        {
            config.ContainerAdapter.AddGeneric(typeof(UnitOfWorkBehavior<,>), typeof(IPipelineBehavior<,>));
            return config;
        }

    }
}
