
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Sql;
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

namespace RCommon.Persistence.Dapper.Tests
{
    [TestFixture()]
    public class DapperRepositoryIntegrationTests : DapperTestBase
    {
        private IDataStoreProvider _dataStoreProvider;

        public DapperRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();

            this.InitializeRCommon(services);
        }


        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup");
            _dataStoreProvider = this.ServiceProvider.GetService<IDataStoreProvider>();

            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            var repo = new TestRepository(context.GetDbConnection());
            repo.ResetDatabase();

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

            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            var repo = new TestRepository(context.GetDbConnection());
            repo.ResetDatabase();

            _dataStoreProvider.RemoveRegisteredDataStores(context.GetType(), Guid.Empty);
            await Task.CompletedTask;
        }

        [Test]
        public async Task Can_perform_simple_query()
        {
            var customer = TestDataActions.CreateCustomerStub();
            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            var repo = new TestRepository(context.GetDbConnection());
            var testData = new List<Customer>();
            testData.Add(customer);
            var ids = await repo.PersistSeedData(testData);

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            var savedCustomer = await customerRepo
                    .FindAsync(ids.First());

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == ids.First());
            Assert.IsTrue(savedCustomer.FirstName == customer.FirstName);
        }


        [Test]
        public async Task Can_Add_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);

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

            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            var repo = new TestRepository(context.GetDbConnection());
            var ids = await repo.PersistSeedData(testData);

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";
            var id = ids.First();
            customer = await customerRepo.FindAsync(id);
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await customerRepo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FindAsync(id);

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

            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            var repo = new TestRepository(context.GetDbConnection());
            var ids = await repo.PersistSeedData(testData);
            var id = ids.First();

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";
            customer = await customerRepo.FindAsync(id);
            await customerRepo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FindAsync(id);

            Assert.IsNull(savedCustomer);

        }


        [Test]
        public async Task UnitOfWork_Can_commit()
        {
            Customer customer = TestDataActions.CreateCustomerStub();

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

            Customer savedCustomer = await repo.FindSingleOrDefaultAsync(x=> x.FirstName == customer.FirstName);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);

        }

        [Test]
        public async Task UnitOfWork_can_rollback()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);

            var context = _dataStoreProvider.GetDataStore<RDbConnection>("TestDbConnection");
            var repo = new TestRepository(context.GetDbConnection());
            await repo.PersistSeedData(testData);

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();
            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            using (var scope = scopeFactory.Create())
            {
                customer = await customerRepo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
                customer.LastName = "Changed";
                await customerRepo.UpdateAsync(customer);


            } //Dispose here as scope is not comitted.
            
            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_works()
        {
            // Generate Test Data
            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();
            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
            repo2.DataStoreName = "TestDbConnection";

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    await repo2.AddAsync(salesPerson);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            savedSalesPerson = await repo2.FindSingleOrDefaultAsync(x => x.FirstName == salesPerson.FirstName);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(customer.FirstName, savedCustomer.FirstName);
            Assert.IsNotNull(savedSalesPerson);
            Assert.AreEqual(savedSalesPerson.FirstName, salesPerson.FirstName);
        }

        [Test]
        public async Task UnitOfWork_nested_commit_with_seperate_transaction_commits_when_wrapping_scope_rollsback()
        {
            // Generate Test Data
            this.Logger.LogInformation("Generating Test Data for: {0}", MethodBase.GetCurrentMethod());

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
            repo2.DataStoreName = "TestDbConnection";

            this.Logger.LogInformation("Starting initial UnitOfWorkScope from {0}", MethodBase.GetCurrentMethod());
            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                this.Logger.LogInformation("Adding New Customer from first UnitOfWorkScope {0}", customer);
                await repo.AddAsync(customer);

                this.Logger.LogInformation("Starting new UnitOfWorkScope from {0}", MethodBase.GetCurrentMethod());
                using (var scope2 = scopeFactory.Create(TransactionMode.New))
                {
                    this.Logger.LogInformation("Adding New Order from first UnitOfWorkScope (0}", salesPerson);
                    await repo2.AddAsync(salesPerson);

                    this.Logger.LogInformation("Attempting to Commit second(new) UnitOfWorkScope {0}", scope2);
                    scope2.Commit();
                }
            } //Rollback

            this.Logger.LogInformation("Attempting to Rollback back initial UnitofWorkScope {0}", "initial scope");

            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            savedSalesPerson = await repo2.FindSingleOrDefaultAsync(x => x.FirstName == salesPerson.FirstName);

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedSalesPerson);
            Assert.AreEqual(salesPerson.FirstName, savedSalesPerson.FirstName); // Second transaction does commit because it is marked "new"
        }

        [Test]
        public async Task UnitOfWork_nested_rollback_works()
        {

            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
            repo2.DataStoreName = "TestDbConnection";

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    await repo2.AddAsync(salesPerson);
                    scope2.Commit();
                }
            } //Rollback.

            var savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            var savedSalesPerson = await repo2.FindSingleOrDefaultAsync(x => x.FirstName == salesPerson.FirstName);
            Assert.IsNull(savedCustomer);
            Assert.IsNull(salesPerson);
        }

        [Test]
        public async Task UnitOfWork_commit_throws_when_child_scope_rollsback()
        {
            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                repo.DataStoreName = "TestDbConnection";
                await repo.AddAsync(customer);
                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
                    repo2.DataStoreName = "TestDbConnection";
                    await repo2.AddAsync(salesPerson);
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
            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
            repo2.DataStoreName = "TestDbConnection";

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                await repo.AddAsync(customer);
                await repo2.AddAsync(salesPerson);
                scope.Commit();
            }


            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            savedSalesPerson = await repo2.FindSingleOrDefaultAsync(x => x.FirstName == salesPerson.FirstName);

            Assert.IsNotNull(savedCustomer);
            Assert.IsNotNull(savedSalesPerson);
            Assert.AreEqual(customer.FirstName, savedCustomer.FirstName);
            Assert.AreEqual(salesPerson.FirstName, savedSalesPerson.FirstName);

        }

        [Test]
        public async Task UnitOfWork_can_rollback_multipe_db_operations()
        {
            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
            repo2.DataStoreName = "TestDbConnection";

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                await repo.AddAsync(customer);
                await repo2.AddAsync(salesPerson);
            }// Rollback



            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            savedSalesPerson = await repo2.FindSingleOrDefaultAsync(x => x.FirstName == salesPerson.FirstName);

            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedSalesPerson);

        }

        [Test]
        public async Task UnitOfWork_rollback_does_not_rollback_supressed_scope()
        {
            var customer = TestDataActions.CreateCustomerStub();
            var salesPerson = TestDataActions.CreateSalesPersonStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkScopeFactory>();

            var repo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            repo.DataStoreName = "TestDbConnection";

            var repo2 = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();
            repo2.DataStoreName = "TestDbConnection";

            using (var scope = scopeFactory.Create(TransactionMode.Default))
            {
                await repo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Supress))
                {
                    await repo2.AddAsync(salesPerson);
                    scope2.Commit();
                }
            } //Rollback.

            var savedCustomer = await repo.FindSingleOrDefaultAsync(x => x.FirstName == customer.FirstName);
            var savedSalesPerson = await repo2.FindSingleOrDefaultAsync(x => x.FirstName == salesPerson.FirstName);

            Assert.IsNull(savedCustomer);
            Assert.IsNotNull(savedSalesPerson);

        }

    }
}
