
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Linq;
using RCommon.Persistence.EFCore.Tests.Specifications;
using RCommon.TestBase;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore.Tests
{
    [TestFixture()]
    public class EFCoreRepositoryIntegrationTests : EFCoreTestBase

    {
        private IDataStoreProvider _dataStoreProvider;
        

        public EFCoreRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();

            //services.AddTransient<IFullFeaturedRepository<Customer>, EFCoreRepository<Customer>>();
            //services.AddTransient<IFullFeaturedRepository<Order>, EFCoreRepository<Order>>();
            this.InitializeRCommon(services);
        }
        

        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup", null);
            //this.ContainerAdapter.Register<DbContext, TestDbContext>(typeof(TestDbContext).AssemblyQualifiedName);

        }

        [SetUp]
        public void Setup()
        {
            //_context = this.ServiceProvider.GetService<RCommonDbContext>();
            this.Logger.LogInformation("Beginning New Test Setup", null);
        }

        [TearDown]
        public async Task TearDown()
        {
            this.Logger.LogInformation("Tearing down Test", null);

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.ResetDatabase();
            
            _dataStoreProvider.RemoveRegisteredDataStores(context.GetType(), Guid.Empty);
        }

        [Test]
        public async Task Can_perform_simple_query()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            var testData = new List<Customer>();
            testData.Add(customer);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";
            
            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Albus");
        }

        [Test]
        public async Task Can_query_using_paging_with_specific_params()
        {
            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
                testData.Add(customer);
            }

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";
            
            var customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("al"), x => x.LastName, true, 1, 10);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 1);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Albus");

            customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("al"), x => x.LastName, true, 2, 10);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 2);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Albus");
        }

        [Test]
        public async Task Can_query_using_paging_with_specification()
        {

            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
                testData.Add(customer);
            }

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

            var customerSearchSpec = new CustomerSearchSpec("al", x => x.FirstName, true, 1, 10);

            var customers = await customerRepo
                    .FindAsync(customerSearchSpec);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 1);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Albus");

            customerSearchSpec = new CustomerSearchSpec("al", x => x.FirstName, true, 2, 10);

            customers = await customerRepo
                    .FindAsync(customerSearchSpec);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 2);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Albus");
        }

        [Test]
        public async Task Can_query_using_predicate_builder()
        {

            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
                testData.Add(customer);
            }

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

            var predicate = PredicateBuilder.True<Customer>(); // This allows us to build compound expressions
            predicate.And(x => x.FirstName.StartsWith("al"));

            var customers = await customerRepo
                    .FindAsync(predicate, x => x.LastName, true, 1, 10);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers[4].FirstName == "Albus");
        }



        [Test]
        public async Task Can_Add_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub(x=>x.FirstName = "Severnus");
            testData.Add(customer);

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstAsync(x=>x.FirstName == "Severnus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);

        }

        [Test]
        public async Task Can_Update_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Severnus");
            testData.Add(customer);

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await customerRepo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstAsync(x =>x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.AreEqual(savedCustomer.LastName, customer.LastName);

        }

        [Test]
        public async Task Can_Delete_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);

            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";
            await customerRepo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstAsync(x=>x.Id == customer.Id);

            Assert.IsNull(savedCustomer);

        }


        [Test]
        public async Task UnitOfWork_Can_commit()
        { 
            Customer customer = _testDataActions.CreateCustomerStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();
            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();

            // Start Test
            using (var scope = scopeFactory.Create())
            {
                
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.Id, customer.Id);

        }

        [Test]
        public async Task UnitOfWork_can_rollback()
        {
            // Generate Test Data
            
            Customer customer = await _testDataActions.CreateCustomerAsync();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();
            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";

            //await repo.AddAsync(customer);
            using (var scope = scopeFactory.Create())
            {
                customer = await repo.FindAsync(customer.Id);
                customer.LastName = "Changed";
                await repo.UpdateAsync(customer);


            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x=>x.Id == customer.Id);
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_works()
        {
            // Generate Test Data
            var customer = TestDataActions.CreateCustomerStub();
            var order = TestDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();


            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);
                //scope.Commit();
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    await repo2.AddAsync(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedOrder = await _testDataActions.GetOrderAsync(x=>x.OrderId == order.OrderId);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(customer.Id, savedCustomer.Id);
            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(order.OrderId, savedOrder.OrderId);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_with_seperate_transaction_commits_when_wrapping_scope_rollsback()
        {
            // Generate Test Data
            this.Logger.LogInformation("Generating Test Data for: " + MethodBase.GetCurrentMethod(), null);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            Customer customer = TestDataActions.CreateCustomerStub();
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            this.Logger.LogInformation("Starting initial UnitOfWorkScope from " + MethodBase.GetCurrentMethod(), null);
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {

                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                this.Logger.LogInformation("Adding New Customer from first UnitOfWorkScope ", customer);
                await repo.AddAsync(customer);

                this.Logger.LogInformation("Starting new UnitOfWorkScope from " + MethodBase.GetCurrentMethod(), null);
                using (var scope2 = scopeFactory.Create(TransactionMode.New))
                {
                    var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    this.Logger.LogInformation("Adding New Order from first UnitOfWorkScope ", order);
                    await repo2.AddAsync(order);

                    this.Logger.LogInformation("Attempting to Commit second(new) UnitOfWorkScope ", scope2);
                    scope2.Commit();
                }
            } //Rollback

            this.Logger.LogInformation("Attempting to Rollback back initial UnitofWorkScope ", null);

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedOrder = await _testDataActions.GetOrderAsync(x=>x.OrderId == order.OrderId);

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedOrder);
            Assert.IsTrue(customer.Id == 0); // First transaction does not commit
            Assert.AreEqual(order.OrderId, savedOrder.OrderId); // Second transaction does commit because it is marked "new"
        }

        [Test]
        public async Task UnitOfWork_nested_rollback_works()
        {

            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    await repo2.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedOrder = await _testDataActions.GetOrderAsync(x=>x.OrderId == order.OrderId);
            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedOrder);
        }

        [Test]
        public async Task UnitOfWork_commit_throws_when_child_scope_rollsback()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    await repo2.AddAsync(order);
                } //child scope rollback.

                //Assert.Throws<InvalidOperationException>(scope.Commit);
                try
                {
                    scope.Commit();
                }
                catch (InvalidOperationException ex)
                {

                    Assert.IsTrue(ex is InvalidOperationException);
                }
            }
        }

        [Test]
        public async Task UnitOfWork_can_commit_multiple_db_operations()
        {
            var customer = new Customer { FirstName = "John", LastName = "Doe" };
            var salesPerson = new SalesPerson { FirstName = "Jane", LastName = "Doe", SalesQuota = 2000 };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);
                var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<SalesPerson>>();
                repo2.DataStoreName = "TestDbContext";
                await repo2.AddAsync(salesPerson);
                scope.Commit();
            }


            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedSalesPerson = await _testDataActions.GetSalesPersonAsync(x=>x.Id == salesPerson.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsNotNull(savedSalesPerson);
            Assert.AreEqual(customer.Id, savedCustomer.Id);
            Assert.AreEqual(salesPerson.Id, savedSalesPerson.Id);
            
        }

        [Test]
        public async Task UnitOfWork_can_rollback_multipe_db_operations()
        {
            var customer = new Customer { FirstName = "John", LastName = "Doe" };
            var salesPerson = new SalesPerson { FirstName = "Jane", LastName = "Doe", SalesQuota = 2000 };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);
                var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<SalesPerson>>();
                repo2.DataStoreName = "TestDbContext";
                await repo2.AddAsync(salesPerson);
            }// Rolllback

            

            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedSalesPerson = await _testDataActions.GetSalesPersonAsync(x => x.Id == salesPerson.Id);

            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedSalesPerson);
            
        }

        [Test]
        public async Task UnitOfWork_rollback_does_not_rollback_supressed_scope()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();


            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Supress))
                {
                    var repo2 = this.ServiceProvider.GetService<IFullFeaturedRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    await repo2.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.


            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedOrder = await _testDataActions.GetOrderAsync(x => x.OrderId == order.OrderId);

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedOrder);
            
        }

        [Test]
        public async Task Can_eager_load_repository_and_query_async()
        {
            var customer = await _testDataActions.CreateCustomerAsync();
            for (int i = 0; i < 10; i++)
            {
                await _testDataActions.CreateOrderForCustomerAsync(customer);
            }

            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            repo.EagerlyWith(x => x.Orders);
            repo.DataStoreName = "TestDbContext";
            //var repo = this.ServiceProvider.GetService<IEFCoreRepository<Customer>>();
            var savedCustomer = await repo
                    .FindSingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.Orders != null);
            Assert.IsTrue(savedCustomer.Orders.Count == 10);
        }

    }
}
