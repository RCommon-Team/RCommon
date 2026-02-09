using Microsoft.Extensions.Caching.Distributed;
using RCommon.Caching;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.RedisCache
{
    /// <summary>
    /// A wrapper for Redis data caching implemented through the
    /// <see cref="IDistributedCache"/> abstraction (backed by StackExchange.Redis).
    /// </summary>
    /// <remarks>
    /// This gives a uniform way for getting/setting cache no matter the caching strategy.
    /// Data is serialized to JSON via <see cref="IJsonSerializer"/> before being stored
    /// in Redis, and deserialized on retrieval.
    /// </remarks>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheService"/> class.
        /// </summary>
        /// <param name="distributedCache">The underlying distributed cache implementation (Redis).</param>
        /// <param name="jsonSerializer">The JSON serializer used to serialize/deserialize cached values.</param>
        public RedisCacheService(IDistributedCache distributedCache, IJsonSerializer jsonSerializer)
        {
            _distributedCache = distributedCache;
            _jsonSerializer = jsonSerializer;
        }

        /// <inheritdoc />
        public TData GetOrCreate<TData>(object key, Func<TData> data)
        {
            var cacheKey = key.ToString()!;
            var json = _distributedCache.GetString(cacheKey);

            if (json == null)
            {
                // Cache miss: invoke the factory, serialize and store the result, then return it
                var result = data();
                _distributedCache.SetString(cacheKey, _jsonSerializer.Serialize(result!));
                return result;
            }
            else
            {
                // Cache hit: deserialize the stored JSON back into the requested type
                return _jsonSerializer.Deserialize<TData>(json)!;
            }
        }

        /// <inheritdoc />
        public async Task<TData> GetOrCreateAsync<TData>(object key, Func<TData> data)
        {
            var cacheKey = key.ToString()!;
            var json = await _distributedCache.GetStringAsync(cacheKey).ConfigureAwait(false);

            if (json == null)
            {
                // Cache miss: invoke the factory, serialize and store the result asynchronously
                var result = data();
                await _distributedCache.SetStringAsync(cacheKey, _jsonSerializer.Serialize(result!)).ConfigureAwait(false);
                return result;
            }
            else
            {
                // Cache hit: deserialize the stored JSON back into the requested type
                return _jsonSerializer.Deserialize<TData>(json)!;
            }
        }
    }
}
