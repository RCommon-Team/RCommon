
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices;
using RCommon.Configuration;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection.Microsoft;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistance.Dapper.Tests
{
    public abstract class DapperTestBase : TestBootstrapper
    {

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .And<EhabExceptionHandlingConfiguration>(x =>
                    x.UsingDefaultExceptionPolicies())
                .And<DataServicesConfiguration>(x=> 
                    x.WithUnitOfWork<DefaultUnitOfWorkConfiguration>())
                .And<DapperConfiguration>(
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
