using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    public static class IInMemoryCachingBuilderExtensions
    {
        public static IInMemoryCachingBuilder Configure(this IInMemoryCachingBuilder builder, Action<MemoryCacheOptions> actions)
        {
            builder.Services.AddMemoryCache(actions);
            return builder;
        }
    }
}
