using RCommon.ApplicationServices;
using RCommon.BusinessServices;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ApplicationServices
{
    public class CommonApplicationServicesConfiguration : IServiceConfiguration
    {
        public CommonApplicationServicesConfiguration()
        {

        }


        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.AddGeneric(typeof(ICrudBusinessService<>), typeof(CrudBusinessService<>));
            containerAdapter.AddGeneric(typeof(ICrudAppService<>), typeof(CrudAppService<,>));
        }
    }
}
