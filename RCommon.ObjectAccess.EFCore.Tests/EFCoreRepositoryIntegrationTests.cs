using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.Domain.Repositories;
using RCommon.ObjectAccess.EFCore;
using System;
using System.Collections.Generic;
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

    }
}