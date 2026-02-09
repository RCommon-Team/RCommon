using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using Microsoft.Extensions.Options;
using RCommon.Core.Threading;
using RCommon.Entities;
using RCommon.Persistence;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db
{
    /// <summary>
    /// Base data connection class for Linq2Db integration with the RCommon persistence layer.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IDataStore"/> to provide a uniform abstraction over data stores,
    /// allowing the <see cref="IDataStoreFactory"/> to resolve named Linq2Db data connections.
    /// Derive from this class instead of <see cref="DataConnection"/> directly when using RCommon.
    /// </remarks>
    public class RCommonDataConnection : DataConnection, IDataStore
    {

        /// <summary>
        /// Initializes a new instance of <see cref="RCommonDataConnection"/> with the specified Linq2Db data options.
        /// </summary>
        /// <param name="linq2DbOptions">The <see cref="DataOptions"/> used to configure the Linq2Db connection.</param>
        public RCommonDataConnection(DataOptions linq2DbOptions)
            :base(linq2DbOptions)
        {

        }



        /// <summary>
        /// Gets the underlying <see cref="DbConnection"/> for this data connection.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/> managed by the Linq2Db <see cref="DataConnection"/>.</returns>
        public DbConnection GetDbConnection()
        {
            return this.Connection;
        }
    }
}
