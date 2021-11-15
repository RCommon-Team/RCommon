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
    public class ApplicationServicesMediatRConfiguration : RCommonConfiguration
    {
        public ApplicationServicesMediatRConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {
            this.ContainerAdapter.AddGeneric(typeof(LoggingBehavior<,>), typeof(IPipelineBehavior<,>));
            this.ContainerAdapter.AddGeneric(typeof(ValidatorBehavior<,,>), typeof(IPipelineBehavior<,>));
            this.ContainerAdapter.AddGeneric(typeof(UnitOfWorkBehavior<,>), typeof(IPipelineBehavior<,>));
        }


    }
}
