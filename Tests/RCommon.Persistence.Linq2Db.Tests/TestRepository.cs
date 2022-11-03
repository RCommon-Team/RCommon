using RCommon.BusinessEntities;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using RCommon.TestBase.Entities;

namespace RCommon.Persistence.Linq2Db.Tests
{
    public class TestRepository
    {
        readonly TestDataConnection _context;
        readonly IList<Action<TestDataConnection>> _entityDeleteActions;

        public TestRepository(TestDataConnection context)
        {
            _context = context;
            _entityDeleteActions = new List<Action<TestDataConnection>>();
        }

        public TestDataConnection Context
        {
            get { return _context; }
        }

        public IList<Action<TestDataConnection>> EntityDeleteActions
        {
            get { return _entityDeleteActions; }
        }

        public void ResetDatabase()
        {
            _context.GetTable<OrderItem>().Delete(x => true); //(Database.ExecuteSqlInterpolated($"DELETE OrderItems");
            _context.GetTable<Product>().Delete(x => true); //_context.Database.ExecuteSqlInterpolated($"DELETE Products");
            _context.GetTable<Order>().Delete(x => true); //_context.Database.ExecuteSqlInterpolated($"DELETE Orders");
            _context.GetTable<Customer>().Delete(x => true); //_context.Database.ExecuteSqlInterpolated($"DELETE Customers");
        }

        public void CleanUpSeedData()
        {
            if (_entityDeleteActions.Count > 0)
            {
                _entityDeleteActions.ForEach(x => x(_context));
            }
        }

        public void PersistSeedData<T>(IList<T> testData)
            where T : BusinessEntity
        {

            foreach (var item in testData)
            {
                _context.Insert<T>(item);
                this.EntityDeleteActions.Add(x => _context.Delete<T>(item));
            }

        }

    }
}
