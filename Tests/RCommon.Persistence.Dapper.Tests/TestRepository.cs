
using RCommon.BusinessEntities;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dommel;
using RCommon.TestBase.Entities;

namespace RCommon.Persistence.Dapper.Tests
{
    public class TestRepository
    {
        readonly IList<Action<DbConnection>> _entityDeleteActions;
        private readonly DbConnection _db;

        public TestRepository(DbConnection db)
        {
            _entityDeleteActions = new List<Action<DbConnection>>();
            _db=db;
        }

        public IList<Action<DbConnection>> EntityDeleteActions
        {
            get { return _entityDeleteActions; }
        }

		public DbConnection Db => this._db;

		public void ResetDatabase()
        {
            _db.DeleteAll<OrderItem>();
            _db.DeleteAll<Product>(); 
            _db.DeleteAll<Order>(); 
            _db.DeleteAll<Customer>(); 
        }

        public void CleanUpSeedData()
        {
            if (_entityDeleteActions.Count > 0)
            {
                _entityDeleteActions.ForEach(x => x(this.Db));
            }
        }

        public async Task<List<int>> PersistSeedData<T>(IList<T> testData)
            where T : BusinessEntity<int>
        {
            var returnIds = new List<int>();
            foreach (var item in testData)
            {
                var objectId = await this.Db.InsertAsync<T>(item);
                int id = Convert.ToInt32(objectId);
                returnIds.Add(id);
                this.EntityDeleteActions.Add(x => x.DeleteAsync<T>(item));
            }
            return returnIds;
        }

    }
}
