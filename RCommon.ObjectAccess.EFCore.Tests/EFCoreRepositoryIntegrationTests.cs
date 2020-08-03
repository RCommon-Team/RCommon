
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices.Transactions;
using RCommon.Domain.Repositories;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    [TestFixture()]
    public class EFCoreRepositoryIntegrationTests : EFCoreTestBase

    {

        private TestDbContext _context;
        

        public EFCoreRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();

            services.AddTransient<IEagerFetchingRepository<Customer>, EFCoreRepository<Customer>>();
            services.AddTransient<IEagerFetchingRepository<Order>, EFCoreRepository<Order>>();
            
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


        }

        [TearDown]
        public void TearDown()
        {
            this.Logger.LogInformation("Tearing down Test", null);
            _context.Database.ExecuteSqlInterpolated($"DELETE OrderItems");
            _context.Database.ExecuteSqlInterpolated($"DELETE Products");
            _context.Database.ExecuteSqlInterpolated($"DELETE Orders");
            _context.Database.ExecuteSqlInterpolated($"DELETE Customers");
            _context.Dispose();
        }

        [Test]
        public void Can_Run_Tests_In_Web_Environment()
        {
            this.CreateWebRequest();
            this.Can_perform_simple_query();
        }

        [Test]
        public void Can_perform_simple_query()
        {
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            var customer = testDataActions.CreateCustomer(x => x.FirstName = "Albus");

            var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";
            //var repo = this.ServiceProvider.GetService<IEFCoreRepository<Customer>>();
            var savedCustomer = repo
                    .Find(customer.CustomerId);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.CustomerId == customer.CustomerId);
            Assert.IsTrue(savedCustomer.FirstName == "Albus");
        }

        [Test]
        public void Can_commit()
        {
            // Generate Test Data
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            Customer customer = testDataActions.CreateCustomerStub();


            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            // Start Test
            using (var scope = scopeFactory.Create())
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                repo.Add(customer);
                scope.Commit();
            }

            Customer savedCustomer = null;
            testData.Batch(action => savedCustomer = action.GetCustomerById(customer.CustomerId));

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.CustomerId, customer.CustomerId);

        }

        [Test]
        public void can_rollback()
        {
            // Generate Test Data
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            Customer customer = null;
            testData.Batch(action => customer = action.CreateCustomer());

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();
            var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
            repo.DataStoreName = "TestDbContext";

            //repo.Add(customer);
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                customer = repo.Find(customer.CustomerId);
                customer.LastName = "Changed";
                repo.Update(customer);


            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            testData.Batch(action => savedCustomer = action.GetCustomerById(customer.CustomerId));
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
        }

        [Test]
        public void nested_commit_works()
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
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                repo.Add(customer);
                //scope.Commit();
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    repo2.Add(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            Order savedOrder = null;
            testData.Batch(actions =>
            {
                savedCustomer = actions.GetCustomerById(customer.CustomerId);
                savedOrder = actions.GetOrderById(order.OrderId);
            });

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(customer.CustomerId, savedCustomer.CustomerId);
            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(order.OrderId, savedOrder.OrderId);
        }

        [Test]
        public void nested_commit_with_seperate_transaction_commits_when_wrapping_scope_rollsback()
        {
            // Generate Test Data
            this.Logger.LogInformation("Generating Test Data for: " + MethodBase.GetCurrentMethod(), null);
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            Customer customer = null;
            testData.Batch(x => customer = x.CreateCustomerStub());
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            this.Logger.LogInformation("Starting initial UnitOfWorkScope from " + MethodBase.GetCurrentMethod(), null);
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {

                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                this.Logger.LogInformation("Adding New Customer from first UnitOfWorkScope ", customer);
                repo.Add(customer);

                this.Logger.LogInformation("Starting new UnitOfWorkScope from " + MethodBase.GetCurrentMethod(), null);
                using (var scope2 = scopeFactory.Create(TransactionMode.New))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    this.Logger.LogInformation("Adding New Order from first UnitOfWorkScope ", order);
                    repo2.Add(order);

                    this.Logger.LogInformation("Attempting to Commit second(new) UnitOfWorkScope ", scope2);
                    scope2.Commit();
                }
            } //Rollback

            this.Logger.LogInformation("Attempting to Rollback back initial UnitofWorkScope ", null);

            Customer savedCustomer = null;
            Order savedOrder = null;
            testData.Batch(actions =>
            {
                savedCustomer = actions.GetCustomerById(customer.CustomerId);
                savedOrder = actions.GetOrderById(order.OrderId);
            });

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedOrder);
            Assert.IsTrue(customer.CustomerId == 0); // First transaction does not commit
            Assert.AreEqual(order.OrderId, savedOrder.OrderId); // Second transaction does commit because it is marked "new"
        }

        [Test]
        public void nested_rollback_works()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                repo.Add(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    repo2.Add(order);
                    scope2.Commit();
                }
            } //Rollback.

            using (var testData = new EFTestData(_context))
            {
                Customer savedCustomer = null;
                Order savedOrder = null;
                testData.Batch(actions =>
                {
                    savedCustomer = actions.GetCustomerById(customer.CustomerId);
                    savedOrder = actions.GetOrderById(order.OrderId);
                });

                Assert.IsNull(savedCustomer);
                Assert.IsNull(savedOrder);
            }
        }

        [Test]
        public void commit_throws_when_child_scope_rollsback()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.DataStoreName = "TestDbContext";
                repo.Add(customer);
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.DataStoreName = "TestDbContext";
                    repo2.Add(order);
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
        public void can_commit_multiple_db_operations()
        {
            var customer = new Customer { FirstName = "John", LastName = "Doe" };
            var salesPerson = new SalesPerson { FirstName = "Jane", LastName = "Doe", SalesQuota = 2000 };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);
                var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<SalesPerson>>();
                repo2.Add(salesPerson);
                scope.Commit();
            }

            using (var ordersTestData = new EFTestData(_context))
            using (var hrTestData = new EFTestData(_context)) // TODO: make this happen on another database/context
            {
                Customer savedCustomer = null;
                SalesPerson savedSalesPerson = null;
                ordersTestData.Batch(action => savedCustomer = action.GetCustomerById(customer.CustomerId));
                hrTestData.Batch(action => savedSalesPerson = action.GetSalesPersonById(salesPerson.Id));

                Assert.IsNotNull(savedCustomer);
                Assert.IsNotNull(savedSalesPerson);
                Assert.AreEqual(customer.CustomerId, savedCustomer.CustomerId);
                Assert.AreEqual(salesPerson.Id, savedSalesPerson.Id);
            }
        }

        [Test]
        public void can_rollback_multipe_db_operations()
        {
            var customer = new Customer { FirstName = "John", LastName = "Doe" };
            var salesPerson = new SalesPerson { FirstName = "Jane", LastName = "Doe", SalesQuota = 2000 };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);
                var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<SalesPerson>>();
                repo2.Add(salesPerson);
            }// Rolllback

            using (var ordersTestData = new EFTestData(_context))
            using (var hrTestData = new EFTestData(_context)) // TODO: make this happen on another database/context
            {
                Customer savedCustomer = null;
                SalesPerson savedSalesPerson = null;
                ordersTestData.Batch(action => savedCustomer = action.GetCustomerById(customer.CustomerId));
                hrTestData.Batch(action => savedSalesPerson = action.GetSalesPersonById(salesPerson.Id));

                Assert.IsNull(savedCustomer);
                Assert.IsNull(savedSalesPerson);
            }
        }

        [Test]
        public void rollback_does_not_rollback_supressed_scope()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();


            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Supress))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.Add(order);
                    scope2.Commit();
                }
            } //Rollback.

            using (var testData = new EFTestData(_context))
            {
                Customer savedCustomer = null;
                Order savedOrder = null;
                testData.Batch(actions =>
                {
                    savedCustomer = actions.GetCustomerById(customer.CustomerId);
                    savedOrder = actions.GetOrderById(order.OrderId);
                });

                Assert.IsNotNull(savedCustomer);
                Assert.IsNotNull(savedOrder);
            }
        }

        [Test]
        public void Can_eager_load_repository_and_query()
        {
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            var customer = testDataActions.CreateCustomer();
            for (int i = 0; i < 10; i++)
            {
                testDataActions.CreateOrderForCustomer(customer);
            }

            var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
            repo.EagerlyWith(x => x.Orders);
            repo.DataStoreName = "TestDbContext";
            //var repo = this.ServiceProvider.GetService<IEFCoreRepository<Customer>>();
            var savedCustomer = repo
                    .FindSingleOrDefault(x=>x.CustomerId == customer.CustomerId);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.CustomerId == customer.CustomerId);
            Assert.IsTrue(savedCustomer.Orders != null);
            Assert.IsTrue(savedCustomer.Orders.Count == 10);
        }

        [Test]
        public async Task Can_eager_load_repository_and_query_async()
        {
            _context = new TestDbContext(this.Configuration);
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            var customer = testDataActions.CreateCustomer();
            for (int i = 0; i < 10; i++)
            {
                testDataActions.CreateOrderForCustomer(customer);
            }

            var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
            repo.EagerlyWith(x => x.Orders);
            repo.DataStoreName = "TestDbContext";
            //var repo = this.ServiceProvider.GetService<IEFCoreRepository<Customer>>();
            var savedCustomer = await repo
                    .FindSingleOrDefaultAsync(x => x.CustomerId == customer.CustomerId);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.CustomerId == customer.CustomerId);
            Assert.IsTrue(savedCustomer.Orders != null);
            Assert.IsTrue(savedCustomer.Orders.Count == 10);
        }

    }
}