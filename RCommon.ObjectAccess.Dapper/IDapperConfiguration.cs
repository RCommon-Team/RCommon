using RCommon.Configuration;
using RCommon.DataServices.Sql;

namespace RCommon.Persistance.Dapper
{
    public interface IDapperConfiguration : IServiceConfiguration
    {
        IDapperConfiguration UsingDbConnection<TDbConnection>() where TDbConnection : IRDbConnection;
    }
}