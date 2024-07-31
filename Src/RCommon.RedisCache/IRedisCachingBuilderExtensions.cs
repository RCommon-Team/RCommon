using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.RedisCache
{
    public static class IRedisCachingBuilderExtensions
    {
        public static IRedisCachingBuilder Configure(this IRedisCachingBuilder builder, Action<RedisCacheOptions> actions)
        {
            builder.Services.AddStackExchangeRedisCache(actions);
            return builder;
        }
    }
}
