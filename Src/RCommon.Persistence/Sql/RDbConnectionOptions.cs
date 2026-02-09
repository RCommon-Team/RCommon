using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sql
{
    /// <summary>
    /// Configuration options for <see cref="RDbConnection"/>, specifying the provider factory and connection string.
    /// </summary>
    public class RDbConnectionOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RDbConnectionOptions"/> class.
        /// </summary>
        public RDbConnectionOptions()
        {

        }

        /// <summary>
        /// Gets or sets the <see cref="DbProviderFactory"/> used to create <see cref="System.Data.Common.DbConnection"/> instances.
        /// </summary>
        public DbProviderFactory DbFactory { get; set; } = default!;

        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        public string ConnectionString { get; set; } = default!;
    }
}
