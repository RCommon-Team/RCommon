
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Sql;
using RCommon.DataServices.Transactions;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper.Tests
{
    [TestFixture()]
    public class DapperRepositoryIntegrationTests : DapperTestBase
    {
        private DapperTestData _testData;
        private DapperTestDataActions _testDataActions;
        private IDataStoreProvider _dataStoreProvider;

        public DapperRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();

            services.AddTransient< ISqlMapperRepository<Customer>, DapperRepository<Customer>>();
            services.AddTransient<ISqlMapperRepository<Order>, DapperRepository<Order>>();
            this.InitializeRCommon(services);
        }


        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup", null);
            //this.ContainerAdapter.Register<DbContext, TestDbConnection>(typeof(TestDbConnection).AssemblyQualifiedName);
            
        }

        [SetUp]
        public void Setup()
        {
            //_context = this.ServiceProvider.GetService<RCommonDbContext>();
            this.Logger.LogInformation("Beginning New Test Setup", null);

            // Setup the context
            _dataStoreProvider = this.ServiceProvider.GetService<IDataStoreProvider>();
            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            _testData = new DapperTestData(context);
            _testDataActions = new DapperTestDataActions(_testData);
        }

        [TearDown]
        public void TearDown()
        {
            this.Logger.LogInformation("Tearing down Test", null);

            _testData.ResetContext();
            _testData.Dispose();
            _dataStoreProvider.RemoveRegisteredDataStores(_testData.GetType(), Guid.NewGuid());
        }

        [Test]
        public async Task Can_Run_Tests_In_Web_Environment()
        {
            this.CreateWebRequest();

            await this.Can_Add_Async();
            await this.Can_Delete_Async();
            await this.Can_perform_simple_query();
            await this.Can_Update_Async();
            await this.UnitOfWork_Can_commit();
            await this.UnitOfWork_can_commit_multiple_db_operations();
        }

        [Test]
        public async Task Can_perform_simple_query()
        {

            var customer = await _testDataActions.CreateCustomerAsync(x => x.FirstName = "Albus");

            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var savedCustomer = await repo.FindSingleOrDefaultAsync($"select * from customers where id = @Id", new List<Parameter>()
                {
                    new Parameter()
                    { DbType = System.Data.DbType.Int32, Direction = System.Data.ParameterDirection.Input, ParameterName = "ID", Value = customer.Id }
                });

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Albus");
        }



        [Test]
        public async Task Can_Add_Async()
        {
            // Generate Test Data
            Customer customer = _testDataActions.CreateCustomerStub(x => x.FirstName = "Severnus");


            // Start Test
            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";
            await repo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.FirstName == "Severnus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);

        }

        [Test]
        public async Task Can_Update_Async()
        {
            // Generate Test Data
            Customer customer = await _testDataActions.CreateCustomerAsync();

            // Start Test
            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await repo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);

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
            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";
            await repo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);

            Assert.IsNull(savedCustomer);

        }


        [Test]
        public async Task UnitOfWork_Can_commit()
        {
            Customer customer = _testDataActions.CreateCustomerStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();
            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();

            // Start Test
            using (var scope = scopeFactory.Create())
            {

                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);

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
            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                
                customer.LastName = "Changed";
                await repo.UpdateAsync(customer);

            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_works()
        {
            // Generate Test Data
            var customer = _testDataActions.CreateCustomerStub();
            var order = _testDataActions.CreateOrderStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();


            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);
                //scope.Commit();
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    repo2.DataStoreName = "TestDbConnection";
                    await repo2.AddAsync(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedOrder = await _testDataActions.GetOrderAsync(x => x.OrderId == order.OrderId);

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

            Customer customer = _testDataActions.CreateCustomerStub();
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            this.Logger.LogInformation("Starting initial UnitOfWorkScope from " + MethodBase.GetCurrentMethod(), null);
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {

                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                this.Logger.LogInformation("Adding New Customer from first UnitOfWorkScope ", customer);
                await repo.AddAsync(customer);

                this.Logger.LogInformation("Starting new UnitOfWorkScope from " + MethodBase.GetCurrentMethod(), null);
                using (var scope2 = scopeFactory.Create(TransactionMode.New))
                {
                    var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    repo2.DataStoreName = "TestDbConnection";
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
            savedOrder = await _testDataActions.GetOrderAsync(x => x.OrderId == order.OrderId);

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
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    repo2.DataStoreName = "TestDbConnection";
                    await repo2.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedOrder = await _testDataActions.GetOrderAsync(x => x.OrderId == order.OrderId);
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
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    repo2.DataStoreName = "TestDbConnection";
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
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);
                var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
                repo2.DataStoreName = "TestDbConnection";
                await repo2.AddAsync(salesPerson);
                scope.Commit();
            }


            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);
            savedSalesPerson = await _testDataActions.GetSalesPersonAsync(x => x.Id == salesPerson.Id);

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
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);
                var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
                repo2.DataStoreName = "TestDbConnection";
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
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Supress))
                {
                    var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    repo2.DataStoreName = "TestDbConnection";
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


    }
}
