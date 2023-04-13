using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCommon.BusinessEntities;
using RCommon.Core.Threading;
using RCommon.DataServices;
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
        private readonly IChangeTracker _changeTracker;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public RCommonDbContext(DbContextOptions options, IChangeTracker changeTracker, IMediator mediator)
            : base(options)
        {
            this._changeTracker = changeTracker;
            this._mediator = mediator;
        }

        public RCommonDbContext(DbContextOptions options, IChangeTracker changeTracker, IMediator mediator, ILogger logger)
            : base(options)
        {
            this._changeTracker = changeTracker;
            this._mediator = mediator;
            this._logger = logger;
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
            this._changeTracker.TrackedEntities.PublishLocalEvents(this._mediator, this._logger);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
