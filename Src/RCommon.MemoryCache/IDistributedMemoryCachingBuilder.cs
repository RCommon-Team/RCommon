using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    /// <summary>
    /// Marker interface for configuring distributed caching that is backed by an in-memory store.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IDistributedCachingBuilder"/> to allow memory-specific distributed
    /// cache configuration via extension methods in <see cref="IDistributedMemoryCachingBuilderExtensions"/>.
    /// </remarks>
    public interface IDistributedMemoryCachingBuilder : IDistributedCachingBuilder
    {
    }
}
