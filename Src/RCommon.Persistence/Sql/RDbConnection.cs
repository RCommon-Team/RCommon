using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RCommon.Core.Threading;
using RCommon.Entities;

namespace RCommon.Persistence.Sql
{
    public class RDbConnection : DisposableResource, IRDbConnection
    {
        private readonly IOptions<RDbConnectionOptions> _options;
        private readonly IEntityEventTracker _entityEventTracker;

        public RDbConnection(IOptions<RDbConnectionOptions> options, IEntityEventTracker entityEventTracker)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            this._entityEventTracker = entityEventTracker ?? throw new ArgumentNullException(nameof(entityEventTracker));
        }

        public DbConnection GetDbConnection()
        {
            Guard.Against<RDbConnectionException>(this._options == null, "No options configured for this RDbConnection");
            Guard.Against<RDbConnectionException>(this._options.Value == null, "No options configured for this RDbConnection");
            Guard.Against<RDbConnectionException>(this._options.Value.DbFactory == null, "You must configured a DbProviderFactory for this RDbConnection");
            Guard.Against<RDbConnectionException>(this._options.Value.ConnectionString.IsNullOrEmpty(), "You must configure a conneciton string for this RDbConnection");

            var connection = this._options.Value.DbFactory.CreateConnection();
            connection.ConnectionString = this._options.Value.ConnectionString;
            
            return connection;
        }

        public async Task PersistChangesAsync()
        {
            await this._entityEventTracker.EmitTransactionalEventsAsync();
        }

    }
}
