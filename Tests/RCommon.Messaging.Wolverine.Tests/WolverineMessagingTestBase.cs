using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Messaging.Wolverine.Tests
{
    public abstract class WolverineMessagingTestBase : TestBootstrapper
    {

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            services.AddRCommon()
                .WithSequentialGuidGenerator(guidOptions =>
                {
                    guidOptions.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString;
                });

            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger<WolverineMessagingTestBase>>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }


        }
    }
}
