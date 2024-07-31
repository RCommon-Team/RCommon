using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Caching.MemoryCaching
{
    public class TestApplicationService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;

        public TestApplicationService(IMemoryCache memoryCache, IDistributedCache distributedCache)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        public async Task SetMemoryCache()
        {

        }

        public async Task GetMemoryCache()
        {

        }

        public async Task SetDistributedMemoryCache()
        {

        }

        public async Task GetDistributedMemoryCache()
        {

        }
    }
}
