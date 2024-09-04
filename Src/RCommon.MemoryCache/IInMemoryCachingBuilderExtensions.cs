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

        /// <summary>
        /// This greatly improves performance across various areas of RCommon which use generics and reflection heavily 
        /// to compile expressions and lambdas
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IInMemoryCachingBuilder CacheDynamicallyCompiledExpressions(this IInMemoryCachingBuilder builder)
        {
            builder.Services.Configure<CachingOptions>(x => 
            { 
                x.CachingEnabled = true; 
                x.CacheDynamicallyCompiledExpressions = true; 
            });
            return builder;
        }
    }
}
