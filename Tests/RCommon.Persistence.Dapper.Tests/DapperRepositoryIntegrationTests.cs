
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Sql;
using RCommon.DataServices.Transactions;
using RCommon.Linq;
using RCommon.Persistence.EFCore;
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

namespace RCommon.Persistence.Dapper.Tests
{
    [TestFixture()]
    public class DapperRepositoryIntegrationTests : DapperTestBase
    {

        public DapperRepositoryIntegrationTests() : base()
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

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == customer.FirstName);
        }

        [Test]
        public async Task Can_Find_Single_Async_With_Expression()
        {

            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Find_Single_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            var savedCustomer = await customerRepo
                    .FindSingleOrDefaultAsync(x=>x.ZipCode == customer.ZipCode);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.ZipCode == customer.ZipCode);
        }

        [Test]
        public async Task Can_Find_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Find_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            var savedCustomers = await customerRepo
                    .FindAsync(x=> x.LastName == "Potter");

            Assert.IsNotNull(savedCustomers);
            Assert.IsTrue(savedCustomers.Count() == 10);
        }

     
        [Test]
        public async Task Can_Get_Count_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Get_Count_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            var savedCustomers = await customerRepo
                    .GetCountAsync(x => x.LastName == "Dumbledore");

            Assert.IsNotNull(savedCustomers);
            Assert.IsTrue(savedCustomers == 10);
        }

        [Test]
        public async Task Can_Get_Any_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Get_Any_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbConnection";

            var canFind = await customerRepo
                    .AnyAsync(x => x.City == "Hollywood");

            Assert.IsTrue(canFind);
        }

        [Test]
        public async Task Can_Use_Default_Data_Store()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Use_Default_DataStore();

            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();

            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Happy");
        }


        [Test]
        public async Task Can_Add_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Add_Async();

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
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Update_Async();


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await customerRepo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().SingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, "Darth");
            Assert.AreEqual(savedCustomer.LastName, "Vader");

        }

        [Test]
        public async Task Can_Delete_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Delete_Async();

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
            await customerRepo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().SingleOrDefaultAsync(x=> x.Id == customer.Id);

            Assert.IsNull(savedCustomer);

        }


        [Test]
        public async Task UnitOfWork_Can_Commit()
        {
            Customer customer = TestDataActions.CreateCustomerStub(x => x.LastName = "Poppadopalus");

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var repo = new TestRepository(this.ServiceProvider);

            // Start Test
            using (var scope = scopeFactory.Create())
            {
                var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();

                await customerRepo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await repo.Context.Set<Customer>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.LastName == "Poppadopalus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.LastName, customer.LastName);

        }

        [Test]
        public async Task UnitOfWork_Can_Rollback()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_UnitOfWork_Can_Rollback();
            var target = customer.LastName;

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

            using (var scope = scopeFactory.Create())
            {
                var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                customer = await customerRepo.FindSingleOrDefaultAsync(x => x.City == customer.City);
                customer.LastName = "Changed";
                await customerRepo.UpdateAsync(customer);

            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == customer.Id);
            Assert.AreEqual(target, savedCustomer.LastName);
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
                var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var orderRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().SingleOrDefaultAsync(x => x.StreetAddress1 == customer.StreetAddress1);
            savedOrder = await repo.Context.Set<Order>().AsNoTracking().SingleOrDefaultAsync(x => x.ShipDate == order.ShipDate);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(customer.StreetAddress1, savedCustomer.StreetAddress1);
            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(order.ShipDate.Value.ToLongDateString(), savedOrder.ShipDate.Value.ToLongDateString());
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
                var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                await customerRepo.AddAsync(customer);

                using (var scope2 = scopeFactory.Create(TransactionMode.Default))
                {
                    var orderRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Order>>();
                    await orderRepo.AddAsync(order);
                    scope2.Commit();
                }
            } //Rollback.

            Customer savedCustomer = null;
            Order savedOrder = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Set<Order>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.Id);
            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedOrder);
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
            

            try
            {
                using (var scope = scopeFactory.Create())
                {
                    var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                    var salesPersonRepo = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();

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
                var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();

                await customerRepo.AddAsync(customer);
                await salesPersonRepo.AddAsync(salesPerson);
                scope.Commit();
            }


            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.FirstName == "Snuffalufagus");
            savedSalesPerson = await repo.Context.Set<SalesPerson>().AsNoTracking().FirstOrDefaultAsync(x => x.FirstName == "Kirby");

            Assert.IsNotNull(savedCustomer);
            Assert.IsNotNull(savedSalesPerson);
            Assert.AreEqual(customer.FirstName, savedCustomer.FirstName);
            Assert.AreEqual(salesPerson.FirstName, savedSalesPerson.FirstName);

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
                var customerRepo = this.ServiceProvider.GetService<ISqlMapperRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<ISqlMapperRepository<SalesPerson>>();

                await customerRepo.AddAsync(customer);
                await salesPersonRepo.AddAsync(salesPerson);
            }// Rollback



            Customer savedCustomer = null;
            SalesPerson savedSalesPerson = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedSalesPerson = await repo.Context.Set<SalesPerson>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == salesPerson.Id);

            Assert.IsNull(savedCustomer);
            Assert.IsNull(savedSalesPerson);

        }

    }
}
