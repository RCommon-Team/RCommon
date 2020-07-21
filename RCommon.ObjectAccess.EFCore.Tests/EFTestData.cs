using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using RCommon.Extensions;

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

        public T Get<T>(Func<T, bool> predicate) where T : class
        {
            //return _context.CreateObjectSet<T>().Where(predicate).FirstOrDefault();
			return _context.Set<T>().Where(predicate).FirstOrDefault();
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
