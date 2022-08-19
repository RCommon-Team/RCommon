using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Configuration;
using RCommon.DependencyInjection.Microsoft;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Tests
{
    public class SecurityTestBase : TestBootstrapper
    {

        public SecurityTestBase()
        {
            
        }

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithClaimsAndPrincipalAccessor();



            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }


        }
    }
}
