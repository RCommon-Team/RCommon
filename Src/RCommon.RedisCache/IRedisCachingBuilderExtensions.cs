using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.RedisCache
{
    /// <summary>
    /// Extension methods for <see cref="IRedisCachingBuilder"/> that configure
    /// Redis cache options and expression caching.
    /// </summary>
    public static class IRedisCachingBuilderExtensions
    {
        /// <summary>
        /// Configures the underlying <see cref="RedisCacheOptions"/> for the StackExchange Redis cache.
        /// </summary>
        /// <param name="builder">The Redis caching builder.</param>
        /// <param name="actions">A delegate to configure <see cref="RedisCacheOptions"/>.</param>
        /// <returns>The same <see cref="IRedisCachingBuilder"/> for chaining.</returns>
        public static IRedisCachingBuilder Configure(this IRedisCachingBuilder builder, Action<RedisCacheOptions> actions)
        {
            builder.Services.AddStackExchangeRedisCache(actions);
            return builder;
        }

        /// <summary>
        /// This greatly improves performance across various areas of RCommon which use generics and reflection heavily
        /// to compile expressions and lambdas
        /// </summary>
        /// <param name="builder">Builder</param>
        /// <returns>Same builder to allow chaining</returns>
        /// <remarks>The most performant way to do this is through InMemoryCache but this works fine</remarks>
        public static IRedisCachingBuilder CacheDynamicallyCompiledExpressions(this IRedisCachingBuilder builder)
        {
            builder.Services.TryAddTransient<ICacheService, RedisCacheService>();
            builder.Services.TryAddTransient<ICommonFactory<ExpressionCachingStrategy, ICacheService>, CommonFactory<ExpressionCachingStrategy, ICacheService>>();

            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });

            // Register factory that resolves the correct ICacheService based on the ExpressionCachingStrategy
            builder.Services.TryAddTransient<Func<ExpressionCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case ExpressionCachingStrategy.Default:
                        return serviceProvider.GetRequiredService<RedisCacheService>();
                    default:
                        return serviceProvider.GetRequiredService<RedisCacheService>();
                }
            });

            return builder;
        }
    }
}
