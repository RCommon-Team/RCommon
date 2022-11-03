using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Linq;
using RCommon.TestBase;
using RCommon.TestBase.Entities;
using RCommon.TestBase.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Linq2Db.Tests
{
    [TestFixture()]
    public class Linq2DbRepositoryIntegrationTests : Linq2DbTestBase
    {
        private IDataStoreRegistry _dataStoreRegistry;


        public Linq2DbRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
        }


        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup");
            _dataStoreRegistry = this.ServiceProvider.GetService<IDataStoreRegistry>();

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

            await Task.CompletedTask;
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDbContext");
            var repo = new TestRepository(context);
            repo.ResetDatabase();
            await Task.CompletedTask;
        }

        [Test]
        public async Task Can_perform_simple_query()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            var testData = new List<Customer>();
            testData.Add(customer);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Albus");
        }

        [Test]
        public async Task Can_use_default_data_store()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Happy");
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            var testData = new List<Customer>();
            testData.Add(customer);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Happy");
        }

        [Test]
        public async Task Can_query_using_paging_with_specific_params()
        {
            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Lisa");
                testData.Add(customer);
            }

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            var customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("li"), x => x.LastName, true, 1, 10);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 1);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Lisa");

            customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("li"), x => x.LastName, true, 2, 10);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 2);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Lisa");
        }

        [Test]
        public async Task Can_query_using_paging_with_specification()
        {

            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Bart");
                testData.Add(customer);
            }

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            var customerSearchSpec = new CustomerSearchSpec("ba", x => x.FirstName, true, 1, 10);

            var customers = await customerRepo
                    .FindAsync(customerSearchSpec);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 1);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Bart");

            customerSearchSpec = new CustomerSearchSpec("ba", x => x.FirstName, true, 2, 10);

            customers = await customerRepo
                    .FindAsync(customerSearchSpec);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers.PageIndex == 2);
            Assert.IsTrue(customers.TotalCount == 100);
            Assert.IsTrue(customers.TotalPages == 10);
            Assert.IsTrue(customers[4].FirstName == "Bart");
        }

        [Test]
        public async Task Can_query_using_predicate_builder()
        {

            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Homer");
                testData.Add(customer);
            }

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            var predicate = PredicateBuilder.True<Customer>(); // This allows us to build compound expressions
            predicate.And(x => x.FirstName.StartsWith("Ho"));

            var customers = await customerRepo
                    .FindAsync(predicate, x => x.LastName, true, 1, 10);

            Assert.IsNotNull(customers);
            Assert.IsTrue(customers.Count == 10);
            Assert.IsTrue(customers[4].FirstName == "Homer");
        }



        [Test]
        public async Task Can_Add_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Severnus");
            testData.Add(customer);


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FirstAsync(x => x.FirstName == "Severnus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);

        }

        [Test]
        public async Task Can_Add_Graph_Async()
        {
            var testData = new List<Customer>();

            string firstName = Guid.NewGuid().ToString();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub(x =>
            {
                x.FirstName = firstName;

                var orders = new List<Order>();
                orders.Add(TestDataActions.CreateOrderStub());
                x.Orders = orders;

            });
            testData.Add(customer);


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.Include(x => x.Orders);
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FirstAsync(x => x.FirstName == firstName);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);
            Assert.IsTrue(savedCustomer.Orders.Count == 1);

        }

        [Test]
        public async Task Can_Update_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await customerRepo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.GetTable<Customer>().FirstAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, "Darth");
            Assert.AreEqual(savedCustomer.LastName, "Vader");

        }

        [Test]
        public async Task Can_Delete_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            await customerRepo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = repo.Context.Customers.Where(x=>x.Id == customer.Id).First();

            Assert.IsNull(savedCustomer);

        }


        [Test]
        public async Task UnitOfWork_Can_commit()
        {
            Customer customer = TestDataActions.CreateCustomerStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            // Start Test
            using (var scope = scopeFactory.Create())
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

                await customerRepo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await repo.Context.Customers.SingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.Id, customer.Id);

        }

        [Test]
        public async Task UnitOfWork_can_rollback()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            using (var scope = scopeFactory.Create())
            {
                customer = await customerRepo.FirstAsync(x => x.Id == customer.Id);
                customer.LastName = "Changed";
                await customerRepo.UpdateAsync(customer);

            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Customers.FirstAsync(x => x.Id == customer.Id);
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_works()
        {
            // Generate Test Data
            var customer = TestDataActions.CreateCustomerStub();
            var order = TestDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var orderRepo = this.ServiceProvider.GetService<IGraphRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Customers.FirstAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Orders.FirstAsync(x => x.OrderId == order.OrderId);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(customer.Id, savedCustomer.Id);
            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(order.OrderId, savedOrder.OrderId);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_with_seperate_transaction_commits_when_wrapping_scope_rollsback()
        {
            // Generate Test Data
            this.Logger.LogInformation("Generating Test Data for: {0}", MethodBase.GetCurrentMethod());

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            Customer customer = TestDataActions.CreateCustomerStub();
            Order order = TestDataActions.CreateOrderStub();

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            this.Logger.LogInformation("Starting initial UnitOfWorkScope from {0}", MethodBase.GetCurrentMethod());
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

                this.Logger.LogInformation("Adding New Customer from first UnitOfWorkScope ", customer);
                await customerRepo.AddAsync(customer);

                this.Logger.LogInformation("Starting new UnitOfWorkScope from {0}", MethodBase.GetCurrentMethod());
                using (var scope2 = scopeFactory.Create(TransactionMode.New))
                {
                    var orderRepo = this.ServiceProvider.GetService<IGraphRepository<Order>>();

                    this.Logger.LogInformation("Adding New Order from first UnitOfWorkScope ", order);
                    await orderRepo.AddAsync(order);

                    this.Logger.LogInformation("Attempting to Commit second(new) UnitOfWorkScope ", scope2);
                    scope2.Commit();
                }
            } //Rollback

            this.Logger.LogInformation("Attempting to Rollback back initial UnitofWorkScope");

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Customers.FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

            Assert.IsNull(savedCustomer); // First transaction does not commit
            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(order.OrderId, savedOrder.OrderId); // Second transaction does commit because it is marked "new"
        }

        [Test]
        public async Task UnitOfWork_nested_rollback_works()
        {

            var customer = TestDataActions.CreateCustomerStub();
            var order = TestDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var orderRepo = this.ServiceProvider.GetService<IGraphRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Customers.FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedOrder);
        }

        [Test]
        public async Task UnitOfWork_commit_throws_when_child_scope_rollsback()
        {
            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            SalesPerson salesPerson = TestDataActions.CreateSalesPersonStub();

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            var salesPersonRepo = this.ServiceProvider.GetService<IGraphRepository<SalesPerson>>();

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

                Assert.IsTrue(ex is TransactionAbortedException);
            }
        }

        [Test]
        public async Task UnitOfWork_can_commit_multiple_db_operations()
        {
            var testData = new List<Customer>();
            var testData2 = new List<SalesPerson>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            SalesPerson salesPerson = TestDataActions.CreateSalesPersonStub();

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<IGraphRepository<SalesPerson>>();

                await customerRepo.AddAsync(customer);
                await salesPersonRepo.AddAsync(salesPerson);
                scope.Commit();
            }


            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.Context.Customers.FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedSalesPerson = await repo.Context.SalesPersons.FirstOrDefaultAsync(x => x.Id == salesPerson.Id);

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
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<IGraphRepository<SalesPerson>>();

                await customerRepo.AddAsync(customer);
                await salesPersonRepo.AddAsync(salesPerson);
            }// Rollback



            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.Context.Customers.FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedSalesPerson = await repo.Context.SalesPersons.FirstOrDefaultAsync(x => x.Id == salesPerson.Id);

            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedSalesPerson);

        }

        [Test]
        public async Task UnitOfWork_rollback_does_not_rollback_supressed_scope()
        {
            var customer = TestDataActions.CreateCustomerStub();
            var order = TestDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Supress))
                {
                    var orderRepo = this.ServiceProvider.GetService<IGraphRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.


            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Customers.FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedOrder);

        }

        [Test]
        public async Task Can_eager_load_repository_and_query_async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            var context = _dataStoreRegistry.GetDataStore<TestDataConnection>("TestDataConnection");
            var repo = new TestRepository(context);
            repo.PersistSeedData(testData);

            var customer = TestDataActions.CreateCustomerStub();
            for (int i = 0; i < 10; i++)
            {
                var order = TestDataActions.CreateOrderStub(x => x.Customer = customer);
                customer.Orders.Add(order);
                testData.Add(customer);
            }
            repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.Include(x => x.Orders);
            var savedCustomer = await customerRepo
                    .FindSingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.Orders != null);
            Assert.IsTrue(savedCustomer.Orders.Count == 10);
        }

    }
}
