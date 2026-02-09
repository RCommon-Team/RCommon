using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    /// <summary>
    /// Marker interface for configuring in-memory caching backed by <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IMemoryCachingBuilder"/> to allow in-memory-specific cache
    /// configuration via extension methods in <see cref="IInMemoryCachingBuilderExtensions"/>.
    /// </remarks>
    public interface IInMemoryCachingBuilder : IMemoryCachingBuilder
    {
    }
}
