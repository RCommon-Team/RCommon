using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    public static class IDistributedMemoryCachingBuilderExtensions
    {
        public static IDistributedMemoryCachingBuilder Configure(this IDistributedMemoryCachingBuilder builder, Action<MemoryDistributedCacheOptions> actions)
        {
            builder.Services.AddDistributedMemoryCache(actions);
            return builder;
        }
    }
}
