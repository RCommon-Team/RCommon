using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using RCommon.Extensions;
using RCommon.DataServices;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    public class EFTestData : IDisposable
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

        public void Dispose()
        {
            if (_entityDeleteActions.Count <= 0)
                return;

            _entityDeleteActions.ForEach(x => x(_context));
            _context.SaveChanges();
            _context.Dispose();
        }
    }
}
