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
        /// <param name="builder">Builder</param>
        /// <returns>Same builder to allow chaining</returns>
        /// <remarks>This is the most performant way to cache expressions!</remarks>
        public static IInMemoryCachingBuilder CacheDynamicallyCompiledExpressions(this IInMemoryCachingBuilder builder)
        {
            builder.Services.TryAddTransient<Func<ExpressionCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case ExpressionCachingStrategy.Default:
                        return serviceProvider.GetService<InMemoryCacheService>();
                    default:
                        return serviceProvider.GetService<InMemoryCacheService>();
                }
            });
            builder.Services.TryAddTransient<ICommonFactory<ExpressionCachingStrategy, ICacheService>, CommonFactory<ExpressionCachingStrategy, ICacheService>>();
            
            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });
            return builder;
        }
    }
}
