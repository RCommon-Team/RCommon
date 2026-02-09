using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    /// <summary>
    /// Defines the contract for configuring in-memory caching services within the RCommon builder pipeline.
    /// </summary>
    /// <seealso cref="IDistributedCachingBuilder"/>
    public interface IMemoryCachingBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register memory caching dependencies.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
