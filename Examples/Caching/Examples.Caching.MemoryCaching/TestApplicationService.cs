using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Caching.MemoryCaching
{
    public class TestApplicationService : ITestApplicationService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly IJsonSerializer _serializer;

        public TestApplicationService(IMemoryCache memoryCache, IDistributedCache distributedCache, IJsonSerializer serializer)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _serializer = serializer;
        }

        public void SetMemoryCache(string key, TestDto data)
        {
            _memoryCache.Set<TestDto>(key, data);
        }

        public TestDto GetMemoryCache(string key)
        {
            return _memoryCache.Get<TestDto>(key);
        }

        public void SetDistributedMemoryCache(string key, Type type, object data)
        {
            _distributedCache.Set(key, Encoding.UTF8.GetBytes(_serializer.Serialize(data, type)));
        }

        public TestDto GetDistributedMemoryCache(string key)
        {
            var cache = _distributedCache.Get(key);
            return _serializer.Deserialize<TestDto>(Encoding.UTF8.GetString(cache));
        }
    }
}
