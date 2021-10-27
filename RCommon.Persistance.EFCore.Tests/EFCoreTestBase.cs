
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Configuration;
using RCommon.DataServices;
using RCommon.DependencyInjection;
using RCommon.DependencyInjection.Microsoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Console;
using System.Transactions;
using RCommon.TestBase;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using RCommon.ApplicationServices;
using RCommon.DataServices.Transactions;

namespace RCommon.Persistance.EFCore.Tests
{
    public abstract class EFCoreTestBase : TestBootstrapper
    {
        
        public EFCoreTestBase()
        {

        }

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            //services.AddDbContext<RCommonDbContext, TestDbContext>();
            //services.AddDbContext<RCommonDbContext, TestDbContext>(ServiceLifetime.Transient);

            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .And<EhabExceptionHandlingConfiguration>(x=>
                    x.UsingDefaultExceptionPolicies())
                .And<DataServicesConfiguration>(x=>
                    x.WithUnitOfWork<DefaultUnitOfWorkConfiguration>())
                .And<EFCoreConfiguration>(x => 
                    x.UsingDbContext<TestDbContext>())
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
