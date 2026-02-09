using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sql
{
    /// <summary>
    /// Represents an ADO.NET-based data store that extends <see cref="IDataStore"/> to provide
    /// raw database connection access for SQL-mapper repositories.
    /// </summary>
    /// <seealso cref="RDbConnection"/>
    /// <seealso cref="Crud.ISqlMapperRepository{TEntity}"/>
    public interface IRDbConnection : IDataStore
    {

    }
}
