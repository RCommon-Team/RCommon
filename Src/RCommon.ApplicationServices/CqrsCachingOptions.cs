using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Configuration options for caching behavior within the CQRS pipeline.
    /// </summary>
    /// <remarks>
    /// When <see cref="UseCacheForHandlers"/> is enabled, the command and query buses may cache
    /// resolved handler metadata to reduce reflection overhead on subsequent dispatches.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public class CqrsCachingOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CqrsCachingOptions"/> with caching disabled.
        /// </summary>
        public CqrsCachingOptions()
        {
            this.UseCacheForHandlers = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether resolved handler metadata should be cached.
        /// </summary>
        public bool UseCacheForHandlers { get; set; }
    }
}
