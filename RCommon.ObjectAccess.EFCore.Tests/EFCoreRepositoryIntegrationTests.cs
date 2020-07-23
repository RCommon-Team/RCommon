using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Domain.Repositories;
using RCommon.ObjectAccess.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    [TestFixture()]
    public class EFCoreRepositoryIntegrationTests : TestBase

    {

        public EFCoreRepositoryIntegrationTests() : base()
        {
            var services = new ServiceCollection();

            services.AddTransient<IEagerFetchingRepository<Customer>, EFCoreRepository<Customer, TestDbContext>>();

            this.InitializeRCommon(services);
        }
        private TestDbContext _context;

        [OneTimeSetUp]
        public void InitialSetup()
        {
            
            //this.ContainerAdapter.Register<DbContext, TestDbContext>(typeof(TestDbContext).AssemblyQualifiedName);
            
            
        }

        [SetUp]
        public void Setup()
        {
            //_context = this.ServiceProvider.GetService<RCommonDbContext>();

            
        }

        [TearDown]
        public void TearDown()
        {
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
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<IUnitOfWorkScope>>();

            // Start Test
            using (var scope = unitOfWorkManager.Create())
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
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

            // Setup required services
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();
            var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();

            using (var testData = new EFTestData(_context))
            {
                Customer customer = null;
                testData.Batch(action => customer = action.CreateCustomer());

                using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
                {
                    var savedCustomer = repo.FindSingleOrDefault(x => x.CustomerId == customer.CustomerId);
                    savedCustomer.LastName = "Changed";
                } //Dispose here as scope is not comitted.

                Customer oldCustomer = null;
                
                testData.Batch(action => oldCustomer = action.GetCustomerById(customer.CustomerId));
                Assert.AreNotEqual("Changed", oldCustomer.LastName);
            }
        }

        [Test]
        public void nested_commit_works()
        {
            // Generate Test Data
            _context = new TestDbContext(this.Configuration);

            // Setup required services
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();

            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };
            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);
                //scope.Commit();
                using (var scope2 = unitOfWorkManager.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.Add(order);
                    scope2.Commit();
                }
                scope.Commit();
            }

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
                Assert.AreEqual(customer.CustomerId, savedCustomer.CustomerId);
                Assert.IsNotNull(savedOrder);
                Assert.AreEqual(order.OrderId, savedOrder.OrderId);
            }
        }

        [Test]
        public void nested_commit_with_seperate_transaction_commits_when_wrapping_scope_rollsback()
        {
            // Generate Test Data
            _context = new TestDbContext(this.Configuration);

            // Setup required services
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();

            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };
            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);

                using (var scope2 = unitOfWorkManager.Create(TransactionMode.New))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
                    repo2.Add(order);
                    scope2.Commit();
                }
            } //Rollback

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
                Assert.AreEqual(order.OrderId, savedOrder.OrderId);
            }
        }

        [Test]
        public void nested_rollback_works()
        {
            var customer = new Customer { FirstName = "Joe", LastName = "Data" };
            var order = new Order { OrderDate = DateTime.Now, ShipDate = DateTime.Now };

            // Setup required services
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();

            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);

                using (var scope2 = unitOfWorkManager.Create(TransactionMode.Default))
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
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();

            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);
                using (var scope2 = unitOfWorkManager.Create(TransactionMode.Default))
                {
                    var repo2 = this.ServiceProvider.GetService<IEagerFetchingRepository<Order>>();
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
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();

            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
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
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();

            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
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
            var unitOfWorkManager = this.ServiceProvider.GetService<ICommonFactory<TransactionMode, IUnitOfWorkScope>>();


            using (var scope = unitOfWorkManager.Create(TransactionMode.Default))
            {
                var repo = this.ServiceProvider.GetService<IEagerFetchingRepository<Customer>>();
                repo.Add(customer);

                using (var scope2 = unitOfWorkManager.Create(TransactionMode.Supress))
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

    }
}