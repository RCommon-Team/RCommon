using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    /// <summary>
    /// Indicates that a component (such as a repository) is associated with a named data source,
    /// allowing it to be resolved from a <see cref="IDataStoreFactory"/> by name.
    /// </summary>
    public interface INamedDataSource
    {
        /// <summary>
        /// Gets or sets the name of the data store this component is associated with.
        /// </summary>
        /// <remarks>
        /// This name is used by <see cref="IDataStoreFactory"/> to resolve the correct <see cref="IDataStore"/> instance.
        /// </remarks>
        public string DataStoreName { get; set; }
    }
}
