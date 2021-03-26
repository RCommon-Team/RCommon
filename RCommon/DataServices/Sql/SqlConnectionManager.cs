using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices.Sql
{
    public class SqlConnectionManager : DisposableResource, ISqlConnectionManager
    {

        public SqlConnectionManager()
        {

        }

        

        public IDbConnection GetSqlDbConnection(string providerInvariantName, string connectionString)
        {
            var factory = DbProviderFactories.GetFactory(providerInvariantName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;
            return connection;
        }

        public async Task PersistChangesAsync()
        {
            // Nothing to do here because this is a SQL Connection
            await Task.CompletedTask;
        }
    }
}
