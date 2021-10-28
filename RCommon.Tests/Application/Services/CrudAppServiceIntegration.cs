using AutoMapper;
using AutoMapper.Configuration;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DataServices;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Tests;
using RCommon.TestBase;
using RCommon.TestBase.Entities;
using RCommon.Tests.Application.DTO;
using RCommon.Tests.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Tests.Application.Services
{
    [TestFixture]
    public class CrudAppServiceIntegration : EFCoreTestBase
    {
        private EFTestData _testData;
        private EFTestDataActions _testDataActions;
        private IDataStoreProvider _dataStoreProvider;

        public CrudAppServiceIntegration() :base()
        {
            var services = new ServiceCollection();

            services.AddTransient<IFullFeaturedRepository<Customer>, EFCoreRepository<Customer>>();
            services.AddTransient<IFullFeaturedRepository<Order>, EFCoreRepository<Order>>();

            services.AddTransient<ITestDomainService, TestDomainService>();
            services.AddTransient<ITestAppService, TestAppService>();

            //IMapperConfigurationExpression expressionMapper = new MapperConfigurationExpression();
            
            //var exp = expressionMapper.CreateMap<CustomerDto, Customer>();
            //expressionMapper.CreateMap<Customer, CustomerDto>();

            services.AddAutoMapper(x =>
            {
                x.CreateMap<CustomerDto, Customer>();
                x.CreateMap<Customer, CustomerDto>();
            });

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
            _dataStoreProvider = this.ServiceProvider.GetService<IDataStoreProvider>();
            var context = _dataStoreProvider.GetDataStore<RCommonDbContext>("TestDbContext");
            _testData = new EFTestData(context);
            _testDataActions = new EFTestDataActions(_testData);
        }

        [TearDown]
        public async Task TearDown()
        {
            this.Logger.LogInformation("Tearing down Test", null);

            await _testData.ResetContext();
            _testData.Dispose();
            _dataStoreProvider.RemoveRegisteredDataStores(_testData.GetType(), Guid.NewGuid());
        }

        [Test]
        public async Task Can_Create_Async()
        {
            
            var customer = _testDataActions.CreateCustomerStub(x=>x.FirstName = "Albus");

            var service = this.ServiceProvider.GetService<ITestAppService>();
            var mapper = this.ServiceProvider.GetService<IMapper>();

            var customerDto = mapper.Map<CustomerDto>(customer);
            var result =  service.CreateAsync(customerDto);

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x=>x.FirstName == "Albus");

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, customer.FirstName);
        }

       

        [Test]
        public async Task Can_Update_Async()
        {
            
            var customer = await _testDataActions.CreateCustomerAsync();

            var service = this.ServiceProvider.GetService<ITestAppService>();
            var mapper = this.ServiceProvider.GetService<IMapper>();

            var customerDto = mapper.Map<CustomerDto>(customer);
            var firstName = new Faker().Name.FirstName();
            customerDto.FirstName = firstName;
            var result = await service.UpdateAsync(customerDto);

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);

            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual(savedCustomer.FirstName, firstName);
        }

        

        [Test]
        public async Task Can_Delete_Async()
        {
            
            var customer = await _testDataActions.CreateCustomerAsync();

            var service = this.ServiceProvider.GetService<ITestAppService>();
            var mapper = this.ServiceProvider.GetService<IMapper>();

            var customerDto = mapper.Map<CustomerDto>(customer);
            var result = service.DeleteAsync(customerDto);

            Customer savedCustomer = null;
            savedCustomer = await _testDataActions.GetCustomerAsync(x => x.Id == customer.Id);

            Assert.IsNull(savedCustomer);
        }

       

        [Test]
        public async Task Can_GetById_Async()
        {
            
            var customer = await _testDataActions.CreateCustomerAsync();

            var service = this.ServiceProvider.GetService<ITestAppService>();

            var result = await service.GetByIdAsync(customer.Id);

            Assert.IsNotNull(result.DataResult);
            Assert.IsTrue(customer.FirstName == result.DataResult.FirstName);
        }

        
    }
}
