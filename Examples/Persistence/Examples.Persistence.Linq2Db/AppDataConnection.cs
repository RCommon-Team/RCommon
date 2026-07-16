using LinqToDB;
using RCommon.Persistence.Linq2Db;

namespace Examples.Persistence.Linq2Db;

public class AppDataConnection : RCommonDataConnection
{
    public AppDataConnection(DataOptions options)
        : base(options)
    {
    }
}
