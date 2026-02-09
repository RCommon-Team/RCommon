using Microsoft.Extensions.Caching.Memory;
using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    /// <summary>
    /// A proxy for in-process memory caching implemented through the
    /// <see cref="IMemoryCache"/> abstraction.
    /// </summary>
    /// <remarks>
    /// This gives a uniform way for getting/setting cache no matter the caching strategy.
    /// Delegates directly to <see cref="IMemoryCache.GetOrCreate{TItem}"/> and its async counterpart.
    /// </remarks>
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCacheService"/> class.
        /// </summary>
        /// <param name="memoryCache">The underlying <see cref="IMemoryCache"/> implementation.</param>
        public InMemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <inheritdoc />
        public TData GetOrCreate<TData>(object key, Func<TData> data)
        {
            return _memoryCache.GetOrCreate<TData>(key, cacheEntry =>
            {
                return data();
            })!;
        }

        /// <inheritdoc />
        public async Task<TData> GetOrCreateAsync<TData>(object key, Func<TData> data)
        {
            return (await _memoryCache.GetOrCreateAsync<TData>(key, async cacheEntry =>
            {
                return await Task.FromResult(data());
            }).ConfigureAwait(false))!;
        }
    }
}
