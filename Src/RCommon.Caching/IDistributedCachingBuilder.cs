using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    /// <summary>
    /// Defines the contract for configuring distributed caching services within the RCommon builder pipeline.
    /// </summary>
    /// <seealso cref="IMemoryCachingBuilder"/>
    public interface IDistributedCachingBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register distributed caching dependencies.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
