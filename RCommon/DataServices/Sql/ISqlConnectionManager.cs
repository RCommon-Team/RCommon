using System.Data;
using System.Threading.Tasks;

namespace RCommon.DataServices.Sql
{
    public interface ISqlConnectionManager : IDataStore
    {
        IDbConnection GetSqlDbConnection(string providerInvariantName, string connectionString);
    }
}