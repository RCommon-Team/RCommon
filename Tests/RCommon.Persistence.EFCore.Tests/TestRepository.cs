using Microsoft.EntityFrameworkCore;
using RCommon.BusinessEntities;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore.Tests
{
    public class TestRepository
    {
        readonly DbContext _context;
        readonly IList<Action<DbContext>> _entityDeleteActions;

        public TestRepository(DbContext context)
        {
            _context = context;
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

    }
}
