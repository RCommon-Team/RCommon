using Microsoft.Extensions.DependencyInjection;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    public static class CachingBuilderExtensions
    {
        public static IRCommonBuilder WithMemoryCaching<T>(this IRCommonBuilder builder)
            where T : IMemoryCachingBuilder
        {
            return WithMemoryCaching<T>(builder, x => { });
        }

        public static IRCommonBuilder WithMemoryCaching<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IMemoryCachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));
            var cachingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(cachingConfig);
            return builder;
        }

        public static IRCommonBuilder WithDistributedCaching<T>(this IRCommonBuilder builder)
            where T : IDistributedCachingBuilder
        {
            return WithDistributedCaching<T>(builder, x => { });
        }

        public static IRCommonBuilder WithDistributedCaching<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IDistributedCachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));
            var cachingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(cachingConfig);
            return builder;
        }

    }
}
