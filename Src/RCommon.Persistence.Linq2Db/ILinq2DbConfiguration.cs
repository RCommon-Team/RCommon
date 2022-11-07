using LinqToDB.Configuration;

namespace RCommon.Persistence.Linq2Db
{
    public interface ILinq2DbConfiguration: IObjectAccessConfiguration
    {
        ILinq2DbConfiguration AddDataConnection<TDataConnection>(string dataStoreName, Action<Linq2DbOptions>? options = null) where TDataConnection : RCommonDataConnection;
    }
}
