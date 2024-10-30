using Microsoft.Extensions.Caching.Distributed;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Caching.RedisCaching
{
    public class TestApplicationService : ITestApplicationService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IJsonSerializer _serializer;

        public TestApplicationService(IDistributedCache distributedCache, IJsonSerializer serializer)
        {
            _distributedCache = distributedCache;
            _serializer = serializer;
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
