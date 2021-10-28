using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using RCommon.Extensions;
using RCommon.DataServices;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore.Tests
{
    public class EFTestData : DisposableResource
    {
        readonly DbContext _context;
        readonly IList<Action<DbContext>> _entityDeleteActions;

        public EFTestData(DbContext context)
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

        public async Task ResetContext()
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE OrderItems");
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE Products");
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE Orders");
            await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE Customers");
        }

        protected override void Dispose(bool disposing)
        {
            if (_entityDeleteActions.Count <= 0)
                

            _entityDeleteActions.ForEach(x => x(_context));
            _context.SaveChanges();
            _context.Dispose();
        }

    }
}
