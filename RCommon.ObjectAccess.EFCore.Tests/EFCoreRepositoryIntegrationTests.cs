using Autofac;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
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

        }
        //private EFCoreRepository<Customer, TestDbContext> _customerRepository;
        private TestDbContext _context;

        [OneTimeSetUp]
        public void InitialSetup()
        {
            
            //this.ContainerAdapter.Register<DbContext, TestDbContext>(typeof(TestDbContext).AssemblyQualifiedName);
            
        }

        [SetUp]
        public void Setup()
        {
            _context = new TestDbContext();
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

        public void Can_Run_Tests_In_Web_Environment()
        {
            this.CreateWebRequest();
        }

        [Test]
        public void Can_perform_simple_query()
        {
            var testData = new EFTestData(_context);
            var testDataActions = new EFTestDataActions(testData);
            var customer = testDataActions.CreateCustomer();

            var repo = (EFCoreRepository<Customer, TestDbContext>)this.AutofacContainer.Resolve(typeof(EFCoreRepository<,>));
            var savedCustomer = repo
                    .Find(customer.CustomerId);

            Assert.IsNotNull(savedCustomer);
        }

    }
}