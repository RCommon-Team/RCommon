using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RCommon.Extensions;

namespace RCommon.DataServices.Sql
{
    public class RDbConnection : DisposableResource, IRDbConnection
    {
        private readonly IOptions<RDbConnectionOptions> _options;

        public RDbConnection(IOptions<RDbConnectionOptions> options)
        {
            _options=options;
        }

        public RDbConnection()
        {

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

        public void PersistChanges()
        { 

            // Nothing to do here because this is a SQL Connection
            return;
        }

    }
}
