using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using MediatR;
using Microsoft.Extensions.Options;
using RCommon.BusinessEntities;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db
{
    public class RCommonDataConnection : DataConnection, IDataStore
    {
        private readonly IEventTracker _eventTracker;
        private readonly IMediator _mediator;

        public RCommonDataConnection(IEventTracker eventTracker, IMediator mediator, DataOptions<RCommonDataConnection> linq2DbOptions)
            :base(linq2DbOptions.Options)
        {
            var options = linq2DbOptions ?? throw new ArgumentNullException(nameof(linq2DbOptions));
            _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            
        }

        public DbConnection GetDbConnection()
        {
            return this.Connection;
        }

        public void PersistChanges()
        {
            this._eventTracker.TrackedEntities.PublishLocalEvents(this._mediator);
            // Nothing to do here because persistence is handled in the underlying API. We'll need to handle Unit of work in a transaction.
            return;
        }
    }
}
