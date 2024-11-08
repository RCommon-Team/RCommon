using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        /// <summary>
        /// This greatly improves performance across various areas of RCommon which use generics and reflection heavily 
        /// to compile expressions and lambdas
        /// </summary>
        /// <param name="builder">Builder</param>
        /// <returns>Same builder to allow chaining</returns>
        /// <remarks>This is the most performant way to cache expressions!</remarks>
        public static IDistributedMemoryCachingBuilder CacheDynamicallyCompiledExpressions(this IDistributedMemoryCachingBuilder builder)
        {

            // Add Caching services
            builder.Services.TryAddTransient<ICacheService, DistributedMemoryCacheService>();
            builder.Services.TryAddTransient<DistributedMemoryCacheService>();
            builder.Services.TryAddTransient<ICommonFactory<ExpressionCachingStrategy, ICacheService>, CommonFactory<ExpressionCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);

            // Add Caching Factory
            builder.Services.TryAddTransient<Func<ExpressionCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case ExpressionCachingStrategy.Default:
                        return serviceProvider.GetService<DistributedMemoryCacheService>();
                    default:
                        return serviceProvider.GetService<DistributedMemoryCacheService>();
                }
            });

            return builder;
        }

        private static void ConfigureCachingOptions(IDistributedMemoryCachingBuilder builder, Action<CachingOptions> configure = null)
        {

            if (configure == null)
            {
                builder.Services.Configure<CachingOptions>(x =>
                {
                    x.CachingEnabled = true;
                    x.CacheDynamicallyCompiledExpressions = true;
                });
            }
            else
            {
                builder.Services.Configure(configure);
            }

        }
    }
}
