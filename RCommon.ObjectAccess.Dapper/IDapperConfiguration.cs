using RCommon.Configuration;
using RCommon.DataServices.Sql;

namespace RCommon.ObjectAccess.Dapper
{
    public interface IDapperConfiguration : IServiceConfiguration
    {
        IDapperConfiguration UsingDbConnection<TDbConnection>() where TDbConnection : IRDbConnection;
    }
}