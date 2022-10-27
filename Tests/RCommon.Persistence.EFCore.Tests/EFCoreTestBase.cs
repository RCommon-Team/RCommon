
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Console;
using System.Transactions;
using RCommon.TestBase;
using RCommon.ApplicationServices;
using RCommon.DataServices.Transactions;
using RCommon.StateStorage;

namespace RCommon.Persistence.EFCore.Tests
{
    public abstract class EFCoreTestBase : TestBootstrapper
    {
        
        public EFCoreTestBase()
        {

        }

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);
            services.AddRCommon()
                .WithStateStorage(new StateStorageConfiguration(), null)
                .WithSequentialGuidGenerator(guid => guid.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString)
                .WithDateTimeSystem(dateTime => dateTime.Kind = DateTimeKind.Utc)
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>(unitOfWork =>
                {
                    unitOfWork.UseDefaultIsolation(IsolationLevel.ReadCommitted);
                    unitOfWork.AutoCompleteScope();
                }) // Everything releated to transaction management. Powerful stuff happens here.
                .WithPersistence<EFCoreConfiguration>(ef => // Repository/ORM configuration. We could easily swap out to NHibernate without impact to domain service up through the stack
                {
                    // Add all the DbContexts here
                    ef.AddDbContext<TestDbContext>(ef =>
                    {
                        ef.UseSqlServer(
                        this.Configuration.GetConnectionString("TestDbContext"));
                    });
                    ef.SetDefaultDataStore(dataStore =>
                    {
                        dataStore.DefaultDataStoreName = "TestDbContext";
                    });
                });

            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger<EFCoreTestBase>>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }


        }

    }
}
