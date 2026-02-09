using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    /// <summary>
    /// Represents an abstraction over a data store (e.g., a database context or connection) that supports async disposal.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface wrap provider-specific data access contexts such as EF Core DbContext
    /// or ADO.NET connections. See <see cref="Sql.RDbConnection"/> for a concrete ADO.NET implementation.
    /// </remarks>
    public interface IDataStore : IAsyncDisposable
    {
        /// <summary>
        /// Gets the underlying <see cref="DbConnection"/> associated with this data store.
        /// </summary>
        /// <returns>A <see cref="DbConnection"/> instance that can be used for direct database operations.</returns>
        DbConnection GetDbConnection();
    }

}
