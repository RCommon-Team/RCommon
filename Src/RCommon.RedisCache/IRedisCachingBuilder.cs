using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.RedisCache
{
    /// <summary>
    /// Marker interface for configuring Redis-backed distributed caching.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IDistributedCachingBuilder"/> to allow Redis-specific cache
    /// configuration via extension methods in <see cref="IRedisCachingBuilderExtensions"/>.
    /// </remarks>
    public interface IRedisCachingBuilder : IDistributedCachingBuilder
    {
    }
}
