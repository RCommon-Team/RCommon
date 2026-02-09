using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    /// <summary>
    /// Defines the strategy used when caching dynamically compiled expressions and lambdas.
    /// </summary>
    /// <remarks>
    /// Used as the key type for <see cref="ICommonFactory{TEnum, TService}"/> to resolve
    /// the appropriate <see cref="ICacheService"/> implementation at runtime.
    /// </remarks>
    public enum ExpressionCachingStrategy
    {
        /// <summary>
        /// The default expression caching strategy.
        /// </summary>
        Default
    }
}
