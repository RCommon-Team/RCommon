using LinqToDB.Configuration;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using LinqToDB.Tools;
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
                linq2Db.AddDataConnection<TestDataConnection>("TestDataConnection", options => CreateLinqToDBConnectionOptions());
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

            //services.AddSingleton<MappingSchema>(x => this.CreateMappingSchema());
            services.AddSingleton<LinqToDBConnectionOptions>(x => CreateLinqToDBConnectionOptions());

            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger<Linq2DbTestBase>>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }


        }

        private LinqToDBConnectionOptions CreateLinqToDBConnectionOptions()
        {
            // create options builder
            var builder = new LinqToDBConnectionOptionsBuilder();

            // configure connection string
            builder.UseSqlServer(this.Configuration.GetConnectionString("TestDataConnection"));
            builder.UseMappingSchema(CreateMappingSchema());
            return new LinqToDBConnectionOptions(builder);
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
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.Orders, customer => customer.Id, order => order.CustomerId)
                .Property(x => x.Id).HasColumnName("CustomerId").IsIdentity();

            builder.Entity<Department>()
                .HasTableName("Departments")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.SalesPersons, department => department.Id, salesPerson => salesPerson.DepartmentId)
                .Property(x => x.Id).IsIdentity();

            builder.Entity<OrderItem>()
                .HasTableName("OrderItems")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.Order, orderItem => orderItem.OrderId, order => order.OrderId)
                .Property(x => x.OrderItemId).IsIdentity();

            builder.Entity<Order>()
                .HasTableName("Orders")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.OrderItems, order => order.OrderId, orderItem => orderItem.OrderId)
                .Property(x => x.OrderId).IsIdentity();

            builder.Entity<Product>()
                .HasTableName("Products")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.OrderItems, product => product.ProductId, orderItem => orderItem.ProductId)
                .Property(x => x.ProductId).IsIdentity();

            return mappingSchema;
        }

    }
}
