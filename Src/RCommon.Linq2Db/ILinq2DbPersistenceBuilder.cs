using LinqToDB;
using LinqToDB.Configuration;

namespace RCommon.Persistence.Linq2Db
{
    public interface ILinq2DbPersistenceBuilder: IPersistenceBuilder
    {
        ILinq2DbPersistenceBuilder AddDataConnection<TDataConnection>(string dataStoreName, Func<IServiceProvider, DataOptions, DataOptions> options) where TDataConnection : RCommonDataConnection;
    }
}
