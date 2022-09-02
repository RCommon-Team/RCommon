using RCommon.Configuration;
using RCommon.DataServices.Sql;
using System;

namespace RCommon
{
    public interface IDapperConfiguration : IObjectAccessConfiguration
    {
        IDapperConfiguration AddDbConnection<TDbConnection>(Action<RDbConnectionOptions> options) where TDbConnection : IRDbConnection;
    }
}
