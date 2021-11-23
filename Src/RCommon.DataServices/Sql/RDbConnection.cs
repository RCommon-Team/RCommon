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

            // Nothing to do here because this is a SQL Connection
            return;
        }

    }
}
