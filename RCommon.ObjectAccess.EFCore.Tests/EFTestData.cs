using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using RCommon.Extensions;
using RCommon.DataServices;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore.Tests
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

        public void Batch(Action<EFTestDataActions> action)
        {
            var dataActions = new EFTestDataActions(this);
            action(dataActions);
            _context.SaveChanges();
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            if (_entityDeleteActions.Count <= 0)
                await Task.Yield();

            _entityDeleteActions.ForEach(x => x(_context));
            await _context.SaveChangesAsync();
            await _context.DisposeAsync();
            await Task.Yield();
        }

    }
}
