using RCommon.ApplicationServices;
using RCommon.BusinessServices;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ApplicationServices
{
    public class CommonApplicationServicesConfiguration : RCommonConfiguration
    {
        public CommonApplicationServicesConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {
            
        }

        public override void Configure()
        {
            this.ContainerAdapter.AddGeneric(typeof(ICrudBusinessService<>), typeof(CrudBusinessService<>));
            this.ContainerAdapter.AddGeneric(typeof(ICrudAppService<>), typeof(CrudAppService<,>));
        }

    }
}
