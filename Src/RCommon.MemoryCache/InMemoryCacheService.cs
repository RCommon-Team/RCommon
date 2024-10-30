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
    /// Just a proxy for memory caching implemented through caching abstractions
    /// </summary>
    /// <remarks>This gives us a uniform way for getting/setting cache no matter the caching strategy</remarks>
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;

        public InMemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public TData GetOrCreate<TData>(object key, Func<TData> data)
        {
            return _memoryCache.GetOrCreate<TData>(key, cacheEntry =>
            {
                return data();
            });
        }

        public async Task<TData> GetOrCreateAsync<TData>(object key, Func<TData> data)
        {
            return await _memoryCache.GetOrCreateAsync<TData>(key, async cacheEntry =>
            {
                return await Task.FromResult(data());
            }).ConfigureAwait(false);
        }
    }
}
