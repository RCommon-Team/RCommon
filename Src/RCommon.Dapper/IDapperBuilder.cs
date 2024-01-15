using RCommon.Persistence.Sql;
using System;

namespace RCommon
{
    public interface IDapperBuilder : IPersistenceBuilder
    {
        IDapperBuilder AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options) where TDbConnection : IRDbConnection;
    }
}
