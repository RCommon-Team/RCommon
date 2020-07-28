using RCommon.Configuration;
using RCommon.DependencyInjection;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ExceptionHandling.EnterpriseLibraryCore
{
    public class EhabExceptionHandlingConfiguration : IExceptionHandlingConfiguration
    {

        public EhabExceptionHandlingConfiguration()
        {

        }

        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.AddTransient<IExceptionManager, EntLibExceptionManager>();
        }
    }
}
