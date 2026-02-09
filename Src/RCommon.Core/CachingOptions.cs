using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Configuration options for controlling caching behavior within RCommon.
    /// Both options default to <c>false</c> (caching disabled).
    /// </summary>
    public class CachingOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CachingOptions"/> with caching disabled by default.
        /// </summary>
        public CachingOptions()
        {
            this.CachingEnabled = false;
            this.CacheDynamicallyCompiledExpressions = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether caching is globally enabled.
        /// </summary>
        public bool CachingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dynamically compiled expressions (e.g., specification predicates)
        /// should be cached to improve performance.
        /// </summary>
        public bool CacheDynamicallyCompiledExpressions { get; set; }
    }
}
