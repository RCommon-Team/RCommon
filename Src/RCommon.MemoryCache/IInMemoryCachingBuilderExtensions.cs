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
    /// Extension methods for <see cref="IInMemoryCachingBuilder"/> that configure
    /// in-memory cache options and expression caching.
    /// </summary>
    public static class IInMemoryCachingBuilderExtensions
    {
        /// <summary>
        /// Configures the underlying <see cref="MemoryCacheOptions"/> for the in-memory cache.
        /// </summary>
        /// <param name="builder">The in-memory caching builder.</param>
        /// <param name="actions">A delegate to configure <see cref="MemoryCacheOptions"/>.</param>
        /// <returns>The same <see cref="IInMemoryCachingBuilder"/> for chaining.</returns>
        public static IInMemoryCachingBuilder Configure(this IInMemoryCachingBuilder builder, Action<MemoryCacheOptions> actions)
        {
            builder.Services.AddMemoryCache(actions);
            return builder;
        }

        /// <summary>
        /// This greatly improves performance across various areas of RCommon which use generics and reflection heavily
        /// to compile expressions and lambdas
        /// </summary>
        /// <param name="builder">Builder</param>
        /// <returns>Same builder to allow chaining</returns>
        /// <remarks>This is the most performant way to cache expressions!</remarks>
        public static IInMemoryCachingBuilder CacheDynamicallyCompiledExpressions(this IInMemoryCachingBuilder builder)
        {

            // Add Caching services
            builder.Services.TryAddTransient<ICacheService, InMemoryCacheService>();
            builder.Services.TryAddTransient<InMemoryCacheService>();
            builder.Services.TryAddTransient<ICommonFactory<ExpressionCachingStrategy, ICacheService>, CommonFactory<ExpressionCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);

            // Add Caching Factory — resolves the correct ICacheService based on the ExpressionCachingStrategy
            builder.Services.TryAddTransient<Func<ExpressionCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case ExpressionCachingStrategy.Default:
                        return serviceProvider.GetRequiredService<InMemoryCacheService>();
                    default:
                        return serviceProvider.GetRequiredService<InMemoryCacheService>();
                }
            });

            return builder;
        }

        /// <summary>
        /// Configures <see cref="CachingOptions"/> with default or custom settings, enabling caching
        /// and expression caching flags.
        /// </summary>
        /// <param name="builder">The in-memory caching builder.</param>
        /// <param name="configure">An optional delegate to customize <see cref="CachingOptions"/>. When <c>null</c>, defaults are applied.</param>
        private static void ConfigureCachingOptions(IInMemoryCachingBuilder builder, Action<CachingOptions>? configure = null)
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
