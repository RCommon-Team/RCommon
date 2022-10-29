using RCommon.DataServices.Sql;
using System;

namespace RCommon
{
    public interface IDapperConfiguration : IObjectAccessConfiguration
    {
        IDapperConfiguration AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options) where TDbConnection : IRDbConnection;
    }
}
