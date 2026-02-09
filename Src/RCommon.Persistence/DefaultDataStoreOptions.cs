using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Options for configuring the default data store name that repositories will use
    /// when no explicit <see cref="RCommon.Persistence.INamedDataSource.DataStoreName"/> is specified.
    /// </summary>
    public class DefaultDataStoreOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDataStoreOptions"/> class.
        /// </summary>
        public DefaultDataStoreOptions()
        {

        }

        /// <summary>
        /// Gets or sets the name of the default data store to be used by repositories.
        /// </summary>
        public string DefaultDataStoreName { get; set; } = default!;
    }
}
