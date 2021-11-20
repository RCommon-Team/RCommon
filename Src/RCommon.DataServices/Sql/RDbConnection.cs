using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Extensions;

namespace RCommon.DataServices.Sql
{
    public class RDbConnection : DisposableResource, IRDbConnection
    {
        private readonly string _providerInvariantName;
        private readonly string _connectionString;

        public event EventHandler<DataStorePersistingEventArgs> DataStorePersisting;

        public RDbConnection(string providerInvariantName, string connectionString)
        {
            _providerInvariantName = providerInvariantName;
            _connectionString = connectionString;
        }

        public RDbConnection()
        {

        }

        public IDbConnection GetDbConnection()
        {
            
            
            var factory = DbProviderFactories.GetFactory(_providerInvariantName);
            
            var connection = factory.CreateConnection();
            connection.ConnectionString = _connectionString;
            return connection;
        }

        public void PersistChanges()
        {
            this.OnDataStorePersisting(new DataStorePersistingEventArgs()); 

            // Nothing to do here because this is a SQL Connection
            return;
        }

        protected virtual void OnDataStorePersisting(DataStorePersistingEventArgs args)
        {
            EventHandler<DataStorePersistingEventArgs> handler = DataStorePersisting;
            if (handler != null)
            {
                handler(this, args);
            }
        }

    }
}
