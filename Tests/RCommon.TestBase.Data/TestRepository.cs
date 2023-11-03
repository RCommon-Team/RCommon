using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RCommon.BusinessEntities;
using RCommon.DataServices;
using RCommon.Extensions;
using RCommon.Persistence.EFCore;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.TestBase.Data
{
    public class TestRepository
    {
        readonly DbContext _context;
        readonly IList<Action<DbContext>> _entityDeleteActions;
        private IDataStoreRegistry _dataStoreRegistry;

        public TestRepository(DbContext context)
        {
            _context = context;
            _entityDeleteActions = new List<Action<DbContext>>();
        }

        public TestRepository(ServiceProvider serviceProvider)
        {
            _dataStoreRegistry = serviceProvider.GetService<IDataStoreRegistry>();
            _context = _dataStoreRegistry.GetDataStore<RCommonDbContext>("TestDbContext");
            _entityDeleteActions = new List<Action<DbContext>>();
        }

        public DbContext Context
        {
            get { return _context; }
        }

        public IList<Action<DbContext>> EntityDeleteActions
        {
            get { return _entityDeleteActions; }
        }

        public void ResetDatabase()
        {
            _context.Database.ExecuteSqlInterpolated($"DELETE OrderItems");
            _context.Database.ExecuteSqlInterpolated($"DELETE Products");
            _context.Database.ExecuteSqlInterpolated($"DELETE Orders");
            _context.Database.ExecuteSqlInterpolated($"DELETE Customers");
        }

        public void CleanUpSeedData()
        {
            if (_entityDeleteActions.Count > 0)
            {
                _entityDeleteActions.ForEach(x => x(_context));
                _context.SaveChanges();
            }
        }

        public void PersistSeedData<T>(IList<T> testData)
            where T : BusinessEntity
        {

            foreach (var item in testData)
            {
                _context.Add<T>(item);
                this.EntityDeleteActions.Add(x => x.Set<T>().Remove(item));
            }

            _context.SaveChanges();

        }

        public Customer Prepare_Can_Find_Async_By_Primary_Key()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
            var testData = new List<Customer>();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

        public Customer Prepare_Can_Find_Single_Async_With_Expression()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.ZipCode = "30062");
            var testData = new List<Customer>();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

        public List<Customer> Prepare_Can_Find_Async_With_Expression()
        {
            var testData = new List<Customer>();
            for (int i = 0; i < 10; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.LastName = "Potter");
                testData.Add(customer);
            }
            this.PersistSeedData(testData);
            return testData;
        }

        public List<Customer> Prepare_Can_Get_Count_Async_With_Expression()
        {
            var testData = new List<Customer>();
            for (int i = 0; i < 10; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.LastName = "Dumbledore");
                testData.Add(customer);
            }
            this.PersistSeedData(testData);
            return testData;
        }

        public List<Customer> Prepare_Can_Get_Any_Async_With_Expression()
        {
            var testData = new List<Customer>();
            for (int i = 0; i < 10; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.City = "Hollywood");
                testData.Add(customer);
            }
            this.PersistSeedData(testData);
            return testData;
        }

        public Customer Prepare_Can_Use_Default_DataStore()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Happy");
            var testData = new List<Customer>();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

        public List<Customer> Prepare_Can_query_using_paging_with_specific_params()
        {
            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Lisa");
                testData.Add(customer);
            }
            this.PersistSeedData(testData);
            return testData;
        }

        public List<Customer> Prepare_Can_query_using_paging_with_specification()
        {

            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Bart");
                testData.Add(customer);
            }
            this.PersistSeedData(testData);
            return testData;

        }

        public List<Customer> Prepare_Can_query_using_predicate_builder()
        {

            var testData = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Homer");
                testData.Add(customer);
            }
            this.PersistSeedData(testData);
            return testData;

        }

        public Customer Prepare_Can_Add_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Severnus");
            testData.Add(customer);
            return customer;
        }

        public Customer Prepare_Can_Add_Graph_Async()
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
            return customer;
        }

        public Customer Prepare_Can_Update_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

        public Customer Prepare_Can_Delete_Async()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

        public Customer Prepare_UnitOfWork_Can_Rollback()
        {
            var testData = new List<Customer>();

            // Generate Test Data
            Customer customer = TestDataActions.CreateCustomerStub();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

    }
}
