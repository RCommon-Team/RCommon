﻿using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.Linq;
using RCommon.TestBase;
using RCommon.TestBase.Data;
using RCommon.TestBase.Entities;
using RCommon.TestBase.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using RCommon.Persistence.Transactions;
using RCommon.Persistence.Crud;

namespace RCommon.Persistence.Linq2Db.Tests
{
    [TestFixture()]
    public class Linq2DbRepositoryIntegrationTests : Linq2DbTestBase
    {
        private IDataStoreFactory _dataStoreFactory;


        public Linq2DbRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }


        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup");
        }

        [SetUp]
        public void Setup()
        {
            this.Logger.LogInformation("Beginning New Test Setup");
        }

        [TearDown]
        public async Task TearDown()
        {
            this.Logger.LogInformation("Tearing down Test");
            var repo = new TestRepository(this.ServiceProvider);
            repo.CleanUpSeedData();
            await Task.CompletedTask;
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            this.Logger.LogInformation("Tearing down Test Suite");
            var repo = new TestRepository(this.ServiceProvider);
            repo.ResetDatabase();
            await Task.CompletedTask;
        }

        [Test]
        public async Task Can_Find_Async_By_Primary_Key()
        {

            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Find_Async_By_Primary_Key();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customerRepo.DataStoreName = "TestDataConnection";

            Assert.ThrowsAsync<NotImplementedException>(async () => await customerRepo
                    .FindAsync(customer.Id));
            /*
             var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);
            Assert.IsNotNull(savedCustomer);
            Assert.That(savedCustomer.Id == customer.Id);
            Assert.That(savedCustomer.FirstName == customer.FirstName);*/
        }

        [Test]
        public async Task Can_Find_Single_Async_With_Expression()
        {

            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Find_Single_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customerRepo.DataStoreName = "TestDataConnection";

            var savedCustomer = await customerRepo
                    .FindSingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.That(savedCustomer != null);
            Assert.That(savedCustomer.Id == customer.Id);
            Assert.That(savedCustomer.ZipCode == customer.ZipCode);
        }

        [Test]
        public async Task Can_Find_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Find_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customerRepo.DataStoreName = "TestDataConnection";

            var savedCustomers = await customerRepo
                    .FindAsync(x => x.LastName == "Potter");

            Assert.That(savedCustomers.Count() == 10);
        }

        [Test]
        public async Task Can_Get_Count_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Get_Count_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customerRepo.DataStoreName = "TestDataConnection";

            var savedCustomers = await customerRepo
                    .GetCountAsync(x => x.LastName == "Dumbledore");

            Assert.That(savedCustomers != null);
            Assert.That(savedCustomers == 10);
        }

        [Test]
        public async Task Can_Get_Any_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Get_Any_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customerRepo.DataStoreName = "TestDataConnection";

            var canFind = await customerRepo
                    .AnyAsync(x => x.City == "Hollywood");

            Assert.That(canFind);
        }

        [Test]
        public async Task Can_Use_Default_Data_Store()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Use_Default_DataStore();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var savedCustomer = await customerRepo
                    .FindSingleOrDefaultAsync(x=>x.Id == customer.Id);

            Assert.That(savedCustomer != null);
            Assert.That(savedCustomer.Id == customer.Id);
            Assert.That(savedCustomer.FirstName == "Happy");
        }

        [Test]
        public async Task Can_Find_Using_Paging_With_Specific_Params()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_paging_with_specific_params();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("Li"), x => x.LastName, true, 1, 10);

            Assert.That(customers.Count == 10);
            Assert.That(customers.PageIndex == 1);
            Assert.That(customers.TotalCount == 100);
            Assert.That(customers.TotalPages == 10);
            Assert.That(customers[4].FirstName == "Lisa");

            customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("li"), x => x.LastName, true, 2, 10);

            Assert.That(customers.Count == 10);
            Assert.That(customers.PageIndex == 2);
            Assert.That(customers.TotalCount == 100);
            Assert.That(customers.TotalPages == 10);
            Assert.That(customers[4].FirstName == "Lisa");

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Find_Using_Paging_With_Specification()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_paging_with_specification();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var customerSearchSpec = new CustomerSearchSpec("ba", x => x.FirstName, true, 1, 10);

            var customers = await customerRepo
                    .FindAsync(customerSearchSpec);

            Assert.That(customers.Count == 10);
            Assert.That(customers.PageIndex == 1);
            Assert.That(customers.TotalCount == 100);
            Assert.That(customers.TotalPages == 10);
            Assert.That(customers[4].FirstName == "Bart");

            customerSearchSpec = new CustomerSearchSpec("ba", x => x.FirstName, true, 2, 10);

            customers = await customerRepo
                    .FindAsync(customerSearchSpec);

            Assert.That(customers.Count == 10);
            Assert.That(customers.PageIndex == 2);
            Assert.That(customers.TotalCount == 100);
            Assert.That(customers.TotalPages == 10);
            Assert.That(customers[4].FirstName == "Bart");

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Find_Using_Predicate_Builder()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_predicate_builder();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var predicate = PredicateBuilder.True<Customer>(); // This allows us to build compound expressions
            predicate.And(x => x.FirstName.StartsWith("Ho"));

            var customers = await customerRepo
                    .FindAsync(predicate, x => x.LastName, true, 1, 10);

            Assert.That(customers.Count == 10);
            Assert.That(customers[4].FirstName == "Homer");

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Query_Using_Paging_With_Specific_Params()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_paging_with_specific_params();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var customers = customerRepo
                    .FindQuery(x => x.FirstName.StartsWith("Li"), x => x.LastName, true, 1, 10);

            Assert.That(customers != null);
            Assert.That(customers.Count() == 10);
            Assert.That(customers.ToList()[4].FirstName == "Lisa");

            customers = customerRepo
                    .FindQuery(x => x.FirstName.StartsWith("li"), x => x.LastName, true, 2, 10);

            Assert.That(customers != null);
            Assert.That(customers.Count() == 10);
            Assert.That(customers.ToList()[4].FirstName == "Lisa");

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Query_Using_Paging_With_Specification()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_paging_with_specification();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var customerSearchSpec = new CustomerSearchSpec("ba", x => x.FirstName, true, 1, 10);

            var customers = customerRepo
                    .FindQuery(customerSearchSpec);

            Assert.That(customers != null);
            Assert.That(customers.Count() == 10);
            Assert.That(customers.ToList()[4].FirstName == "Bart");

            customerSearchSpec = new CustomerSearchSpec("ba", x => x.FirstName, true, 2, 10);

            customers = customerRepo
                    .FindQuery(customerSearchSpec);

            Assert.That(customers != null);
            Assert.That(customers.Count() == 10);

            Assert.That(customers.ToList()[4].FirstName == "Bart");

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Query_Using_Predicate_Builder()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_predicate_builder();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            var predicate = PredicateBuilder.True<Customer>(); // This allows us to build compound expressions
            predicate.And(x => x.FirstName.StartsWith("Ho"));

            var customers = customerRepo
                    .FindQuery(predicate, x => x.LastName, true, 1, 10);

            Assert.That(customers != null);
            Assert.That(customers.Count() == 10);
            Assert.That(customers.ToList()[4].FirstName == "Homer");

            repo.CleanUpSeedData();
        }



        [Test]
        public async Task Can_Add_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Add_Async();

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FirstAsync(x => x.FirstName == "Severnus");

            Assert.That(savedCustomer.FirstName == customer.FirstName);
            Assert.That(savedCustomer.Id > 0);

        }

        [Test]
        public async Task Can_Update_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Update_Async();


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await customerRepo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstAsync(x => x.Id == customer.Id);

            Assert.That(savedCustomer.FirstName == "Darth");
            Assert.That(savedCustomer.LastName == "Vader");

        }

        [Test]
        public async Task Can_Delete_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Delete_Async();

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            await customerRepo.DeleteAsync(customer);

            Customer savedCustomer = null;
            repo.Context.ChangeTracker.Clear();
            savedCustomer = await repo.Context.Set<Customer>().SingleOrDefaultAsync(x=>x.Id == customer.Id);

            Assert.That(savedCustomer == null);

        }


        [Test]
        public async Task UnitOfWork_Can_Commit()
        {
            Customer customer = TestDataActions.CreateCustomerStub(x => x.LastName = "Griswold");

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var repo = new TestRepository(this.ServiceProvider);

            // Start Test
            using (var scope = scopeFactory.Create())
            {
                var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

                await customerRepo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await repo.Context.Set<Customer>()
                .SingleOrDefaultAsync(x => x.LastName == "Griswold");

            Assert.That(savedCustomer.LastName == customer.LastName);

        }

        [Test]
        public async Task UnitOfWork_Can_Rollback()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_UnitOfWork_Can_Rollback();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();

            using (var scope = scopeFactory.Create())
            {
                customer = await customerRepo.FirstAsync(x => x.Id == customer.Id);
                customer.LastName = "Changed";
                await customerRepo.UpdateAsync(customer);

            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstAsync(x => x.Id == customer.Id);
            Assert.That(customer.LastName != savedCustomer.LastName);
        }

        [Test]
        public async Task UnitOfWork_Nested_Commit_Works()
        {
            // Generate Test Data
            var customer = TestDataActions.CreateCustomerStub();
            var order = TestDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var repo = new TestRepository(this.ServiceProvider);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var orderRepo = this.ServiceProvider.GetService<ILinqRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Set<Customer>().SingleOrDefaultAsync(x => x.StreetAddress1 == customer.StreetAddress1);
            savedOrder = await repo.Context.Set<Order>().SingleOrDefaultAsync(x => x.ShipDate == order.ShipDate);

            Assert.That(customer.StreetAddress1 == savedCustomer.StreetAddress1);
            Assert.That(order.ShipDate.Value.ToLongDateString() == savedOrder.ShipDate.Value.ToLongDateString());
        }

        [Test]
        public async Task UnitOfWork_Nested_Rollback_Works()
        {

            var customer = TestDataActions.CreateCustomerStub();
            var order = TestDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var repo = new TestRepository(this.ServiceProvider);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var orderRepo = this.ServiceProvider.GetService<ILinqRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Set<Order>().FirstOrDefaultAsync(x => x.Id == order.Id);
            Assert.That(savedCustomer == null);
            Assert.That(savedOrder == null);
        }

        [Test]
        public async Task UnitOfWork_Commit_Throws_When_Child_Scope_Rollsback()
        {
            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            SalesPerson salesPerson = TestDataActions.CreateSalesPersonStub();

            var repo = new TestRepository(this.ServiceProvider);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            var salesPersonRepo = this.ServiceProvider.GetService<ILinqRepository<SalesPerson>>();

            try
            {
                using (var scope = scopeFactory.Create())
                {
                    await customerRepo.AddAsync(customer);
                    using (var scope2 = scopeFactory.Create())
                    {
                        await salesPersonRepo.AddAsync(salesPerson);
                    } //child scope rollback.

                }
            }
            catch (Exception ex)
            {

                Assert.That(ex is TransactionAbortedException);
            }
        }

        [Test]
        public async Task UnitOfWork_Can_Commit_Multiple_Db_Operations()
        {
            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Snuffalufagus");
            SalesPerson salesPerson = TestDataActions.CreateSalesPersonStub(x => x.FirstName = "Kirby");

            var repo = new TestRepository(this.ServiceProvider);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<ILinqRepository<SalesPerson>>();

                await customerRepo.AddAsync(customer);
                await salesPersonRepo.AddAsync(salesPerson);
                scope.Commit();
            }


            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstOrDefaultAsync(x => x.FirstName == "Snuffalufagus");
            savedSalesPerson = await repo.Context.Set<SalesPerson>().FirstOrDefaultAsync(x => x.FirstName == "Kirby");

            Assert.That(customer.FirstName == savedCustomer.FirstName);
            Assert.That(salesPerson.FirstName == savedSalesPerson.FirstName);

        }

        [Test]
        public async Task UnitOfWork_Can_Rollback_Multipe_Db_Operations()
        {
            var customer = new Customer { FirstName = "John", LastName = "Doe" };
            var salesPerson = new SalesPerson { FirstName = "Jane", LastName = "Doe", SalesQuota = 2000 };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            var repo = new TestRepository(this.ServiceProvider);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<ILinqRepository<SalesPerson>>();

                await customerRepo.AddAsync(customer);
                await salesPersonRepo.AddAsync(salesPerson);
            }// Rollback



            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedSalesPerson = await repo.Context.Set<SalesPerson>().FirstOrDefaultAsync(x => x.Id == salesPerson.Id);

            Assert.That(savedCustomer == null);
            Assert.That(savedSalesPerson == null);

        }

        
        [Test]
        public async Task Can_Eager_Load_Repository_And_Query_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            var repo = new TestRepository(this.ServiceProvider);
            repo.PersistSeedData(testData);

            var customer = TestDataActions.CreateCustomerStub();
            for (int i = 0; i < 10; i++)
            {
                var order = TestDataActions.CreateOrderStub(x => x.Customer = customer);
                customer.Orders.Add(order);
                testData.Add(customer);
            }
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<ILinqRepository<Customer>>();
            customerRepo.Include(x => x.Orders);
            var savedCustomer = await customerRepo
                    .FindSingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.That(savedCustomer.Id == customer.Id);
            Assert.That(savedCustomer.Orders != null);
            Assert.That(savedCustomer.Orders.Count == 10);
        }

    }
}
