using Microsoft.Extensions.Options;
using RCommon.Persistence.Sql;

namespace Examples.Persistence.Dapper;

public class AppDbConnection : RDbConnection
{
    public AppDbConnection(IOptions<RDbConnectionOptions> options)
        : base(options)
    {
    }
}
