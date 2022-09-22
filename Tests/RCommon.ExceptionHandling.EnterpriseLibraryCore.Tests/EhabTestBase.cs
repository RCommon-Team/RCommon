using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.DependencyInjection.Microsoft;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RCommon.ExceptionHandling.EnterpriseLibraryCore.Tests
{
    public abstract class EhabTestBase : TestBootstrapper
    {

        public EhabTestBase() : base()
        {

        }

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .And<EhabExceptionHandlingConfiguration>(x=>
                    x.UsingDefaultExceptionPolicies());



            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger<EhabTestBase>>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }

        }

    }
}
