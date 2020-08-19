using RCommon.Application.Services;
using RCommon.DependencyInjection;
using RCommon.Domain.DomainServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Configuration
{
    public class CommonApplicationServicesConfiguration : IServiceConfiguration
    {
        public CommonApplicationServicesConfiguration()
        {

        }


        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.AddGeneric(typeof(ICrudDomainService<>), typeof(CrudDomainService<>));
            containerAdapter.AddGeneric(typeof(ICrudAppService<>), typeof(CrudAppService<,>));
        }
    }
}
