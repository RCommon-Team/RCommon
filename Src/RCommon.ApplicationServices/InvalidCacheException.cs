using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Exception thrown when the caching infrastructure is not properly configured or available.
    /// </summary>
    /// <remarks>
    /// This exception is raised by <see cref="Commands.CommandBus"/> or <see cref="Queries.QueryBus"/>
    /// when expression caching is enabled but the required <c>ICommonFactory&lt;ExpressionCachingStrategy, ICacheService&gt;</c>
    /// cannot be resolved from the service provider.
    /// </remarks>
    public class InvalidCacheException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InvalidCacheException"/> with the specified error message.
        /// </summary>
        /// <param name="message">A message describing the caching configuration problem.</param>
        public InvalidCacheException(string message):base(message)
        {

        }
    }
}
