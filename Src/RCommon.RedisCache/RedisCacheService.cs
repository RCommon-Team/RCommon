﻿using Microsoft.Extensions.Caching.Distributed;
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
    /// Just a wrapper for Redis data caching implemented through caching abstractions
    /// </summary>
    /// <remarks>This gives us a uniform way for getting/setting cache no matter the caching strategy</remarks>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IJsonSerializer _jsonSerializer;

        public RedisCacheService(IDistributedCache distributedCache, IJsonSerializer jsonSerializer)
        {
            _distributedCache = distributedCache;
            _jsonSerializer = jsonSerializer;
        }

        public TData GetOrCreate<TData>(object key, Func<TData> data)
        {
            var json = _distributedCache.GetString(key.ToString());

            if (json == null)
            {
                _distributedCache.SetString(key.ToString(), _jsonSerializer.Serialize(data()));
                return data();
            }
            else
            {
                return _jsonSerializer.Deserialize<TData>(json);
            }
        }

        public async Task<TData> GetOrCreateAsync<TData>(object key, Func<TData> data)
        {
            var json = await _distributedCache.GetStringAsync(key.ToString()).ConfigureAwait(false);

            if (json == null)
            {
                await _distributedCache.SetStringAsync(key.ToString(), _jsonSerializer.Serialize(data())).ConfigureAwait(false);
                return data();
            }
            else
            {
                return _jsonSerializer.Deserialize<TData>(json);
            }
        }
    }
}
