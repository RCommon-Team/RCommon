using RCommon.Persistence.Sql;
using System;

namespace RCommon
{
    public interface IDapperConfiguration : IPersistenceConfiguration
    {
        IDapperConfiguration AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options) where TDbConnection : IRDbConnection;
    }
}
