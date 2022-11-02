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
        private readonly LinqToDBConnectionOptions _linq2DbOptions;
        private readonly IChangeTracker _changeTracker;
        private readonly IMediator _mediator;

        public RCommonDataConnection(IChangeTracker changeTracker, IMediator mediator, IOptions<LinqToDBConnectionOptions> linq2DbOptions)
            :base()
        {
            var options = linq2DbOptions ?? throw new ArgumentNullException(nameof(linq2DbOptions));
            _linq2DbOptions = linq2DbOptions.Value ?? throw new ArgumentNullException(nameof(linq2DbOptions.Value));
            _changeTracker = changeTracker ?? throw new ArgumentNullException(nameof(changeTracker));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public DbConnection GetDbConnection()
        {
            return this.GetDbConnection();
        }

        public void PersistChanges()
        {
            this._changeTracker.TrackedEntities.PublishLocalEvents(this._mediator);
            // Nothing to do here because persistence is handled in the underlying API. We'll need to handle Unit of work in a transaction.
            return;
        }
    }
}
