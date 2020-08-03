using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using RCommon.ExceptionHandling.EnterpriseLibraryCore.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            //containerAdapter.AddSingleton<IConfigurationSource, DictionaryConfigurationSource>();

           
        }
    }
}
