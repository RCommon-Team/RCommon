
using RCommon.BusinessEntities;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dommel;

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
            //_context.Database.ExecuteSqlInterpolated($"DELETE OrderItems");
            //_context.Database.ExecuteSqlInterpolated($"DELETE Products");
            //_context.Database.ExecuteSqlInterpolated($"DELETE Orders");
            //_context.Database.ExecuteSqlInterpolated($"DELETE Customers");
        }

        public void CleanUpSeedData()
        {
            if (_entityDeleteActions.Count > 0)
            {
                _entityDeleteActions.ForEach(x => x(this.Db));
            }
        }

        public async Task PersistSeedData<T>(IList<T> testData)
            where T : BusinessEntity
        {
            await using (var db = this.Db)
            {

            }
                foreach (var item in testData)
            {
                await this.Db.InsertAsync<T>(item);
                this.EntityDeleteActions.Add(x => x.DeleteAsync<T>(item));
            }

        }

    }
}
