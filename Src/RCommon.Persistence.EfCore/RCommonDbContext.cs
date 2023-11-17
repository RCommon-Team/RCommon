using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.Entities;
using RCommon.Core.Threading;
using RCommon.Persistence;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public abstract class RCommonDbContext : DbContext, IDataStore
    {
        private readonly IEventTracker _eventTracker;
        private readonly IMediatorService _mediator;

        public RCommonDbContext(DbContextOptions options, IEventTracker eventTracker, IMediatorService mediator)
            : base(options)
        {
            this._eventTracker = eventTracker;
            this._mediator = mediator;
        }

        public RCommonDbContext(DbContextOptions options)
            : base(options)
        {

        }

        

        public DbConnection GetDbConnection()
        {
            return base.Database.GetDbConnection();
        }

        public virtual void PersistChanges()
        {
            AsyncHelper.RunSync(() => this.SaveChangesAsync(true));
        }


        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            this._eventTracker.TrackedEntities.PublishLocalEvents(this._mediator);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
