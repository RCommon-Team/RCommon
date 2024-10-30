using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.Configuration;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Persistence.Transactions;
using RCommon.TestBase;
using RCommon.TestBase.Data;
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
                .WithPersistence<EFCorePerisistenceBuilder>(ef => // Repository/ORM configuration. We could easily swap out to NHibernate without impact to domain service up through the stack
                {
                    // Add all the DbContexts here
                    ef.AddDbContext<TestDbContext>("TestDbContext", ef =>
                    {
                        ef.UseSqlServer(
                        this.Configuration.GetConnectionString("TestDbContext"));
                    });
                })
                .WithPersistence<Linq2DbPersistenceBuilder>(linq2Db =>
                {
                    // Add all the DbContexts here
                    linq2Db.AddDataConnection<TestDataConnection>("TestDataConnection", (provider, options) =>
                    {
                        return options
                            .UseSqlServer(this.Configuration.GetConnectionString("TestDataConnection"))
                            .UseMappingSchema(CreateMappingSchema())
                            .UseDefaultLogging(this.ServiceProvider);

                    });
                    linq2Db.SetDefaultDataStore(dataStore =>
                    {
                        dataStore.DefaultDataStoreName = "TestDataConnection";
                    });
                })
                .WithUnitOfWork<DefaultUnitOfWorkBuilder>(unitOfWork =>
                {
                    unitOfWork.SetOptions(options =>
                    {
                        options.AutoCompleteScope = false;
                        options.DefaultIsolation = System.Transactions.IsolationLevel.ReadCommitted;
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

        private MappingSchema CreateMappingSchema()
        {
            // IMPORTANT: configure mapping schema instance only once
            // and use it with all your connections that need those mappings
            // Never create new mapping schema for each connection as
            // it will seriously harm performance https://linq2db.github.io/#fluent-configuration

            var mappingSchema = new MappingSchema();
            var builder = new FluentMappingBuilder(mappingSchema);



            builder.Entity<Customer>()
                .HasTableName("Customers")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.Orders, customer => customer.Id, order => order.CustomerId)
                .Property(x => x.Id).HasColumnName("CustomerId").IsIdentity().IsPrimaryKey()
                .Property(x => x.City).HasColumnName("City")
                .Property(x => x.FirstName).HasColumnName("FirstName")
                .Property(x => x.LastName).HasColumnName("LastName");

            builder.Entity<Department>()
                .HasTableName("Departments")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.SalesPersons, department => department.Id, salesPerson => salesPerson.DepartmentId)
                .Property(x => x.Id).IsIdentity().IsPrimaryKey();

            builder.Entity<OrderItem>()
                .HasTableName("OrderItems")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.Order, orderItem => orderItem.OrderId, order => order.Id)
                .Property(x => x.OrderItemId).HasColumnName("OrderItemId").IsIdentity().IsPrimaryKey();

            builder.Entity<Order>()
                .HasTableName("Orders")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.OrderItems, order => order.Id, orderItem => orderItem.OrderId)
                .Property(x => x.Id).HasColumnName("OrderId").IsIdentity().IsPrimaryKey()
                .Property(x => x.OrderDate).HasColumnName("OrderDate")
                .Property(x => x.ShipDate).HasColumnName("ShipDate");

            builder.Entity<Product>()
                .HasTableName("Products")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.OrderItems, product => product.ProductId, orderItem => orderItem.ProductId)
                .Property(x => x.ProductId).HasColumnName("ProductId").IsIdentity().IsPrimaryKey();

            builder.Entity<SalesPerson>()
                .HasTableName("SalesPerson")
                .HasSchemaName("dbo")
                .Ignore(x => x.AllowEventTracking)
                .Association(e => e.Department, salesPerson => salesPerson.DepartmentId, department => department.Id)
                .Property(x => x.Id).IsIdentity().IsPrimaryKey();

            builder.Build();
            return mappingSchema;
        }

    }
}
