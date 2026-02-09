using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching
{
    /// <summary>
    /// Defines the strategy used when caching persistence (repository) query results.
    /// </summary>
    /// <remarks>
    /// Used as the key type for <see cref="ICommonFactory{TEnum, TService}"/> to resolve
    /// the appropriate <see cref="ICacheService"/> implementation for caching repositories at runtime.
    /// </remarks>
    public enum PersistenceCachingStrategy
    {
        /// <summary>
        /// The default persistence caching strategy.
        /// </summary>
        Default
    }
}
