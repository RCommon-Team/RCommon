
using Dapper.FluentMap.Dommel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Persistence.Dapper.Tests.Configurations;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Dapper.Tests
{
    public abstract class DapperTestBase : TestBootstrapper
    {

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);

            services.AddRCommon()
                .WithPersistence<DapperConfiguration, DefaultUnitOfWorkConfiguration>(dapper =>
                {
                    
                    dapper.AddDbConnection<TestDbConnection>("TestDbConnection", db =>
                    {
                        db.DbFactory = SqlClientFactory.Instance;
                        db.ConnectionString = this.Configuration.GetConnectionString("TestDbConnection");
                    });
                    dapper.AddFluentMappings(mappings =>
                    {
                        mappings.AddMap(new CustomerMap());
                        mappings.AddMap(new SalesPersonMap());
                        mappings.ForDommel();
                    });
                    dapper.SetDefaultDataStore(dataStore =>
                    {
                        dataStore.DefaultDataStoreName = "TestDbConnection";
                    });
                }, unitOfWork =>
                {
                    unitOfWork.SetOptions(options =>
                    {
                        options.AutoCompleteScope = true;
                        options.DefaultIsolation = System.Transactions.IsolationLevel.ReadCommitted;
                    });
                });

            

            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger<DapperTestBase>>();

            // Retrieve the installed providers and factories.
            DataTable table = DbProviderFactories.GetFactoryClasses();

            // Display each row and column value.
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    Debug.WriteLine("Sql Provider: {0}", row[column]);
                }
            }

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }


        }
    }
}
