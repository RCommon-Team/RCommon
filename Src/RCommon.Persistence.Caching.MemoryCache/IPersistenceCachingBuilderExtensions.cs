using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using RCommon.MemoryCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.MemoryCache
{
    public static class IPersistenceCachingBuilderExtensions
    {
        public static IPersistenceCachingBuilder AddInMemoryPersistenceCaching(this IPersistenceCachingBuilder builder)
        {
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetService<InMemoryCacheService>();
                    default:
                        return serviceProvider.GetService<InMemoryCacheService>();
                }
            });
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();

            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });
            return builder;
        }

        public static IPersistenceCachingBuilder AddDistributedMemoryPersistenceCaching(this IPersistenceCachingBuilder builder)
        {
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetService<DistributedMemoryCacheService>();
                    default:
                        return serviceProvider.GetService<DistributedMemoryCacheService>();
                }
            });
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();

            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });
            return builder;
        }
    }
}
