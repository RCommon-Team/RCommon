using LinqToDB.Configuration;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.TestBase;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static LinqToDB.DataProvider.SqlServer.SqlServerProviderAdapter;

namespace RCommon.Persistence.Linq2Db.Tests
{
    public class Linq2DbTestBase : TestBootstrapper
    {

        public Linq2DbTestBase()
        {

        }

        protected void InitializeRCommon(IServiceCollection services)
        {

            base.InitializeBootstrapper(services);
            services.AddRCommon()
                .WithSequentialGuidGenerator(guid => guid.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString)
                .WithDateTimeSystem(dateTime => dateTime.Kind = DateTimeKind.Utc)
                .WithPersistence<Linq2DbConfiguration, DefaultUnitOfWorkConfiguration>(linq2Db => 
                {
                    // Add all the DbContexts here
                    linq2Db.AddDataConnection<TestDataConnection>("TestDataConnection", options => CreateLinq2DbBuilder());
                    linq2Db.SetDefaultDataStore(dataStore =>
                    {
                        dataStore.DefaultDataStoreName = "TestDataConnection";
                    });
                }, unitOfWork =>
                {
                    unitOfWork.SetOptions(options =>
                    {
                        options.AutoCompleteScope = false;
                        options.DefaultIsolation = IsolationLevel.ReadCommitted;
                    });
                });

            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger<Linq2DbTestBase>>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }


        }

        private LinqToDBConnectionOptionsBuilder CreateLinq2DbBuilder()
        {
            // create options builder
            var builder = new LinqToDBConnectionOptionsBuilder();

            // configure connection string
            builder.UseSqlServer(this.Configuration.GetConnectionString("TestDataConnection"));
            builder.UseMappingSchema(CreateMappingSchema());
            return builder;
        }

        private MappingSchema CreateMappingSchema()
        {
            // IMPORTANT: configure mapping schema instance only once
            // and use it with all your connections that need those mappings
            // Never create new mapping schema for each connection as
            // it will seriously harm performance https://linq2db.github.io/#fluent-configuration
            var mappingSchema = new MappingSchema();
            var builder = mappingSchema.GetFluentMappingBuilder();

            builder.Entity<Customer>()
                .HasTableName("Customers")
                .HasSchemaName("dbo")
                .HasIdentity(x => x.Id)
                .HasPrimaryKey(x => x.Id)
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.Orders, customer => customer.Id, order => order.CustomerId);

            return mappingSchema;
        }

    }
}
