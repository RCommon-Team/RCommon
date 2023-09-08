using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        public Customer Prepare_Can_Perform_Simple_Query()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Albus");
            var testData = new List<Customer>();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

        public Customer Prepare_Can_Use_Default_DataStore()
        {
            var customer = TestDataActions.CreateCustomerStub(x => x.FirstName = "Happy");
            var testData = new List<Customer>();
            testData.Add(customer);
            this.PersistSeedData(testData);
            return customer;
        }

    }
}
