using LinqToDB.Configuration;

namespace RCommon.Persistence.Linq2Db
{
    public interface ILinq2DbConfiguration: IObjectAccessConfiguration
    {
        ILinq2DbConfiguration AddDataConnection<TDataConnection>(string dataStoreName, Func<IServiceProvider, LinqToDBConnectionOptions> options) where TDataConnection : RCommonDataConnection;
    }
}
