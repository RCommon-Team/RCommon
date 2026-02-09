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
    /// <summary>
    /// Extension methods for <see cref="IDistributedMemoryCachingBuilder"/> that configure
    /// distributed memory cache options and expression caching.
    /// </summary>
    public static class IDistributedMemoryCachingBuilderExtensions
    {
        /// <summary>
        /// Configures the underlying <see cref="MemoryDistributedCacheOptions"/> for the distributed memory cache.
        /// </summary>
        /// <param name="builder">The distributed memory caching builder.</param>
        /// <param name="actions">A delegate to configure <see cref="MemoryDistributedCacheOptions"/>.</param>
        /// <returns>The same <see cref="IDistributedMemoryCachingBuilder"/> for chaining.</returns>
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

            // Add Caching Factory — resolves the correct ICacheService based on the ExpressionCachingStrategy
            builder.Services.TryAddTransient<Func<ExpressionCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case ExpressionCachingStrategy.Default:
                        return serviceProvider.GetRequiredService<DistributedMemoryCacheService>();
                    default:
                        return serviceProvider.GetRequiredService<DistributedMemoryCacheService>();
                }
            });

            return builder;
        }

        /// <summary>
        /// Configures <see cref="CachingOptions"/> with default or custom settings, enabling caching
        /// and expression caching flags.
        /// </summary>
        /// <param name="builder">The distributed memory caching builder.</param>
        /// <param name="configure">An optional delegate to customize <see cref="CachingOptions"/>. When <c>null</c>, defaults are applied.</param>
        private static void ConfigureCachingOptions(IDistributedMemoryCachingBuilder builder, Action<CachingOptions>? configure = null)
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
