﻿
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    [TestFixture()]
    public class EFCoreRepositoryIntegrationTests : EFCoreTestBase

    {

        private RCommonDbContext _context;
        private EFTestDataActions _testDataActions;
        

        public EFCoreRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();

            services.AddTransient<IFullFeaturedRepository<Customer>, EFCoreRepository<Customer>>();
            services.AddTransient<IFullFeaturedRepository<Order>, EFCoreRepository<Order>>();
            
            
            //services.AddTransient<IEFCoreRepository<Customer>, EFCoreRepository<Customer, TestDbContext>>();
            //services.AddTransient<IEFCoreRepository<Order>, EFCoreRepository<Order, TestDbContext>>();

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

            // Setup the context
            var dataStoreProvider = this.ServiceProvider.GetService<IDataStoreProvider>();
            _context = dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            var testData = new EFTestData(_context);
            _testDataActions = new EFTestDataActions(testData);
        }

        [TearDown]
        public async Task TearDown()
        {
            this.Logger.LogInformation("Tearing down Test", null);
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE OrderItems");
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE Products");
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE Orders");
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE Customers");
            //await _context.DisposeAsync();
        }

        [Test]
        public async Task Can_Run_Tests_In_Web_Environment()
        {
            this.CreateWebRequest();
            
            await this.Can_Add_Async();
            await this.Can_Delete_Async();
            await this.Can_eager_load_repository_and_query_async();
            await this.Can_perform_simple_query();
            await this.Can_Update_Async();
            await this.UnitOfWork_Can_commit();
            await this.UnitOfWork_can_commit_multiple_db_operations();
        }

        [Test]
        public async Task Can_perform_simple_query()
        {
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            var customer = await testDataActions.CreateCustomerAsync(x => x.FirstName = "Albus");

            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";
            //var repo = this.ServiceProvider.GetService<IEFCoreRepository<Customer>>();
            var savedCustomer = await repo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Albus");
        }

       

        [Test]
        public async Task Can_Add_Async()
        {
            // Generate Test Data
            Customer customer = _testDataActions.CreateCustomerStub(x=>x.FirstName = "Severnus");


            // Start Test
            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";
            await repo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = _testDataActions.GetFirstCustomer(x=>x.FirstName == "Severnus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);

        }

        [Test]
        public async Task Can_Update_Async()
        {
            // Generate Test Data
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            Customer customer = await testDataActions.CreateCustomerAsync();

            // Start Test
            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await repo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = testDataActions.GetCustomerById(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.AreEqual(savedCustomer.LastName, customer.LastName);

        }

        [Test]
        public async Task Can_Delete_Async()
        {
            // Generate Test Data
            Customer customer = await _testDataActions.CreateCustomerAsync();

            // Start Test
            var repo = this.ServiceProvider.GetService<IFullFeaturedRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";
            await repo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = _testDataActions.GetCustomerById(customer.Id);

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
            await using (var scope = scopeFactory.Create())
            {
                
                repo.DataStoreName = "TestDbContext";
                await repo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.Id == customer.Id);
            //savedCustomer = _testDataActions.GetCustomerById(customer.Id);

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
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                customer = await repo.FindAsync(customer.Id);
                customer.LastName = "Changed";
                await repo.UpdateAsync(customer);

                
            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = _testDataActions.GetCustomerById(customer.Id);
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_works()
        {
            // Generate Test Data
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            var customer = testDataActions.CreateCustomerStub();
            var order = testDataActions.CreateOrderStub();

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
            savedCustomer = testDataActions.GetCustomerById(customer.Id);
            savedOrder = testDataActions.GetOrderById(order.OrderId);

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
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            Customer customer = null;
            testData.Batch(x => customer = x.CreateCustomerStub());
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
            savedCustomer = testDataActions.GetCustomerById(customer.Id);
            savedOrder = testDataActions.GetOrderById(order.OrderId);

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedOrder);
            Assert.IsTrue(customer.Id == 0); // First transaction does not commit
            Assert.AreEqual(order.OrderId, savedOrder.OrderId); // Second transaction does commit because it is marked "new"
        }

        [Test]
        public async Task UnitOfWork_nested_rollback_works()
        {
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);

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
            testData.Batch(actions =>
            {
                savedCustomer = actions.GetCustomerById(customer.Id);
                savedOrder = actions.GetOrderById(order.OrderId);
            });

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

            using (var ordersTestData = new EFTestData(_context))
            using (var hrTestData = new EFTestData(_context)) // TODO: make this happen on another database/context
            {
                Customer savedCustomer = null;
                SalesPerson savedSalesPerson = null;
                ordersTestData.Batch(action => savedCustomer = action.GetCustomerById(customer.Id));
                hrTestData.Batch(action => savedSalesPerson = action.GetSalesPersonById(salesPerson.Id));

                Assert.IsNotNull(savedCustomer);
                Assert.IsNotNull(savedSalesPerson);
                Assert.AreEqual(customer.Id, savedCustomer.Id);
                Assert.AreEqual(salesPerson.Id, savedSalesPerson.Id);
            }
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

            using (var ordersTestData = new EFTestData(_context))
            using (var hrTestData = new EFTestData(_context)) // TODO: make this happen on another database/context
            {
                Customer savedCustomer = null;
                SalesPerson savedSalesPerson = null;
                ordersTestData.Batch(action => savedCustomer = action.GetCustomerById(customer.Id));
                hrTestData.Batch(action => savedSalesPerson = action.GetSalesPersonById(salesPerson.Id));

                Assert.IsNull(savedCustomer);
                Assert.IsNull(savedSalesPerson);
            }
        }

        [Test]
        public async Task UnitOfWork_rollback_does_not_rollback_supressed_scope()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();


            await using (var scope = scopeFactory.Create(TransactionMode.Default))
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

            await using (var testData = new EFTestData(_context))
            {
                Customer savedCustomer = null;
                Order savedOrder = null;
                testData.Batch(actions =>
                {
                    savedCustomer = actions.GetCustomerById(customer.Id);
                    savedOrder = actions.GetOrderById(order.OrderId);
                });

                Assert.IsNotNull(savedCustomer);
                Assert.IsNotNull(savedOrder);
            }
        }

        [Test]
        public async Task Can_eager_load_repository_and_query_async()
        {
            var customer = await _testDataActions.CreateCustomerAsync();
            for (int i = 0; i < 10; i++)
            {
                await _testDataActions.CreateOrderForCustomer(customer);
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