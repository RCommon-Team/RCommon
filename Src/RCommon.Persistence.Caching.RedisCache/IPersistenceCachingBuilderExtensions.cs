﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using RCommon.Persistence.Caching.Crud;
using RCommon.RedisCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.RedisCache
{
    public static class IPersistenceCachingBuilderExtensions
    {

        public static void AddRedisPersistenceCaching(this IPersistenceBuilder builder)
        {
            // Add Caching repositories
            builder.Services.TryAddTransient(typeof(ICachingGraphRepository<>), typeof(CachingGraphRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingLinqRepository<>), typeof(CachingLinqRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingSqlMapperRepository<>), typeof(CachingSqlMapperRepository<>));

            // Add Caching services
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetService<RedisCacheService>();
                    default:
                        return serviceProvider.GetService<RedisCacheService>();
                }
            });
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();

            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });
        }
    }
}
