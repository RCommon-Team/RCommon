using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.Entities;
using RCommon.Core.Threading;
using RCommon.Persistence;
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
        private readonly IEntityEventTracker _entityEventTracker;


        public RCommonDbContext(DbContextOptions options, IEntityEventTracker entityEventTracker)
            : base(options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this._entityEventTracker = entityEventTracker ?? throw new ArgumentNullException(nameof(entityEventTracker));
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
            await this._entityEventTracker.EmitTransactionalEventsAsync();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
