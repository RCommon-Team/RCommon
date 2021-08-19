using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Configuration;
using RCommon.DependencyInjection.Microsoft;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.Dapper.Tests
{
    public abstract class DapperTestBase : TestBootstrapper
    {

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            //services.AddDbContext<RCommonDbContext, TestDbContext>();
            //services.AddDbContext<RCommonDbContext, TestDbContext>(ServiceLifetime.Transient);

            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .WithExceptionHandling<EhabExceptionHandlingConfiguration>(x =>
                    x.UsingDefaultExceptionPolicies())
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>()
                .WithObjectAccess<DapperConfiguration>(
                    x => x.UsingDbConnection<TestDbConnection>())
                .And<CommonApplicationServicesConfiguration>();



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
