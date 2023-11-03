
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Linq;
using RCommon.TestBase;
using RCommon.TestBase.Data;
using RCommon.TestBase.Entities;
using RCommon.TestBase.Specifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.EFCore.Tests
{
    [TestFixture()]
    public class EFCoreRepositoryIntegrationTests : EFCoreTestBase

    {
        

        public EFCoreRepositoryIntegrationTests() : base()
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

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";
            
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

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.ZipCode == customer.ZipCode);
        }

        [Test]
        public async Task Can_Find_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Find_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

            var savedCustomers = await customerRepo
                    .FindAsync(x => x.LastName == "Potter");

            Assert.IsNotNull(savedCustomers);
            Assert.IsTrue(savedCustomers.Count() == 10);
        }

        [Test]
        public async Task Can_Get_Count_Async_With_Expression()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customers = repo.Prepare_Can_Get_Count_Async_With_Expression();

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

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

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.DataStoreName = "TestDbContext";

            var canFind = await customerRepo
                    .AnyAsync(x => x.City == "Hollywood");

            Assert.IsTrue(canFind);
        }

        [Test]
        public async Task Can_Use_Default_Data_Store()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Use_Default_DataStore();

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            var savedCustomer = await customerRepo
                    .FindAsync(customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsTrue(savedCustomer.Id == customer.Id);
            Assert.IsTrue(savedCustomer.FirstName == "Happy");
        }

        [Test]
        public async Task Can_Query_Using_Paging_With_Specific_Params()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_paging_with_specific_params();

            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            
            var customers = await customerRepo
                    .FindAsync(x => x.FirstName.StartsWith("Li"), x => x.LastName, true, 1, 10);

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

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Query_Using_Paging_With_Specification()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_paging_with_specification();

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

            repo.CleanUpSeedData();
        }

        [Test]
        public async Task Can_Query_Using_Predicate_Builder()
        {
            var repo = new TestRepository(this.ServiceProvider);
            repo.Prepare_Can_query_using_predicate_builder();

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
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Add_Async();

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FirstAsync(x=>x.FirstName == "Severnus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);

        }

        [Test]
        public async Task Can_Add_Graph_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Add_Graph_Async();

            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customerRepo.Include(x => x.Orders);
            await customerRepo.AddAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await customerRepo.FirstAsync(x => x.FirstName == customer.FirstName);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
            Assert.IsTrue(savedCustomer.Id > 0);
            Assert.IsTrue(savedCustomer.Orders.Count == 1);

        }

        [Test]
        public async Task Can_Update_Async()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_Can_Update_Async();


            // Start Test
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            customer.FirstName = "Darth";
            customer.LastName = "Vader";
            await customerRepo.UpdateAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FirstAsync(x =>x.Id == customer.Id);

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
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
            await customerRepo.DeleteAsync(customer);

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().FindAsync(customer.Id);

            Assert.IsNull(savedCustomer);

        }


        [Test]
        public async Task UnitOfWork_Can_Commit()
        { 
            Customer customer = TestDataActions.CreateCustomerStub();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var repo = new TestRepository(this.ServiceProvider);

            // Start Test
            using (var scope = scopeFactory.Create())
            {
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

                await customerRepo.AddAsync(customer);
                scope.Commit();
            }

            Customer savedCustomer = await repo.Context.Set<Customer>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.Id, customer.Id);

        }

        [Test]
        public async Task UnitOfWork_Can_Rollback()
        {
            var repo = new TestRepository(this.ServiceProvider);
            var customer = repo.Prepare_UnitOfWork_Can_Rollback();

            // Setup required services
            var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();
            var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

            using (var scope = scopeFactory.Create())
            {
                customer = await customerRepo.FirstAsync(x=>x.Id == customer.Id);
                customer.LastName = "Changed";
                await customerRepo.UpdateAsync(customer);

            } //Dispose here as scope is not comitted.

            Customer savedCustomer = null;
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstAsync(x=>x.Id == customer.Id);
            Assert.AreNotEqual(customer.LastName, savedCustomer.LastName);
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
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Set<Order>().AsNoTracking().FirstAsync(x=>x.Id == order.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(customer.Id, savedCustomer.Id);
            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(order.Id, savedOrder.Id);
        }

        /// <summary>
        /// This won't work due to EF's ability/need to commit all changes on the graph. The original intent of this 
        /// test was to evaluate transactions across mulitple connections but that is no longer possible due to lack of DTC. 
        /// Going forward, this likely needs to be tested as part of a SAGA unit of work. 
        /// </summary>
        /// <returns></returns>
        //[Test]
        //public async Task UnitOfWork_Nested_Commit_With_Seperate_Transaction_Commits_When_Wrapping_Scope_Rollsback()
        //{
        //    // Generate Test Data
        //    this.Logger.LogInformation("Generating Test Data for: {0}", MethodBase.GetCurrentMethod());

        //    // Setup required services
        //    var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

        //    Customer customer = TestDataActions.CreateCustomerStub();
        //    Order order = TestDataActions.CreateOrderStub();

        //    var repo = new TestRepository(this.ServiceProvider);

        //    this.Logger.LogInformation("Starting initial UnitOfWorkScope from {0}", MethodBase.GetCurrentMethod());
        //    using (var scope = scopeFactory.Create(TransactionMode.Default))
        //    {
        //        var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();

        //        this.Logger.LogInformation("Adding New Customer from first UnitOfWorkScope ", customer);
        //        await customerRepo.AddAsync(customer);

        //        this.Logger.LogInformation("Starting new UnitOfWorkScope from {0}", MethodBase.GetCurrentMethod());
        //        using (var scope2 = scopeFactory.Create(TransactionMode.New))
        //        {
        //            var orderRepo = this.ServiceProvider.GetService<IGraphRepository<Order>>();

        //            this.Logger.LogInformation("Adding New Order from first UnitOfWorkScope ", order);
        //            await orderRepo.AddAsync(order);

        //            this.Logger.LogInformation("Attempting to Commit second(new) UnitOfWorkScope ", scope2);
        //            scope2.Commit();
        //        }
        //    } //Rollback

        //    this.Logger.LogInformation("Attempting to Rollback back initial UnitofWorkScope");

        //    Customer savedCustomer = null;
        //    Order savedOrder = null;
        //    savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
        //    savedOrder = await repo.Context.Set<Order>().AsNoTracking().FirstOrDefaultAsync(x=>x.OrderId == order.OrderId);

        //    Assert.IsNull(savedCustomer); // First transaction does not commit
        //    Assert.IsNotNull(savedOrder);
        //    Assert.AreEqual(order.OrderId, savedOrder.OrderId); // Second transaction does commit because it is marked "new"
        //}

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
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedOrder = await repo.Context.Set<Order>().AsNoTracking().FirstOrDefaultAsync(x=>x.Id == order.Id);
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
        public async Task UnitOfWork_Can_Commit_Multiple_Db_Operations()
        {
            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            SalesPerson salesPerson = TestDataActions.CreateSalesPersonStub();

            var repo = new TestRepository(this.ServiceProvider);

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
            savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
            savedSalesPerson = await repo.Context.Set<SalesPerson>().AsNoTracking().FirstOrDefaultAsync(x=>x.Id == salesPerson.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.IsNotNull(savedSalesPerson);
            Assert.AreEqual(customer.Id, savedCustomer.Id);
            Assert.AreEqual(salesPerson.Id, savedSalesPerson.Id);
            
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
                var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
                var salesPersonRepo = this.ServiceProvider.GetService<IGraphRepository<SalesPerson>>();

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

        /// <summary>
        /// This won't work due to EF's ability/need to commit all changes on the graph. The original intent of this 
        /// test was to evaluate transactions across mulitple connections but that is no longer possible due to lack of DTC. 
        /// Going forward, this likely needs to be tested as part of a SAGA unit of work. 
        /// </summary>
        //[Test]
        //public async Task UnitOfWork_Rollback_Does_Not_Rollback_Supressed_Scope()
        //{
        //    var customer = TestDataActions.CreateCustomerStub();
        //    var order = TestDataActions.CreateOrderStub();

        //    // Setup required services
        //    var scopeFactory = this.ServiceProvider.GetService<IUnitOfWorkFactory>();

        //    var repo = new TestRepository(this.ServiceProvider);

        //    using (var scope = scopeFactory.Create(TransactionMode.Default))
        //    {
        //        var customerRepo = this.ServiceProvider.GetService<IGraphRepository<Customer>>();
        //        await customerRepo.AddAsync(customer);

        //        using (var scope2 = scopeFactory.Create(TransactionMode.Supress))
        //        {
        //            var orderRepo = this.ServiceProvider.GetService<IGraphRepository<Order>>();
        //            await orderRepo.AddAsync(order);
        //            scope2.Commit();
        //        }
        //    } //Rollback.


        //    Customer savedCustomer = null;
        //    Order savedOrder = null;
        //    savedCustomer = await repo.Context.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
        //    savedOrder = await repo.Context.Set<Order>().AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

        //    Assert.IsNull(savedCustomer);
        //    Assert.IsNotNull(savedOrder);

        //}

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
