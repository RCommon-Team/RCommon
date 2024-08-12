using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public static class ICqrsBuilderExtensions
    {
        public static ICqrsBuilder AddMemoryCachingForHandlers<T>(this ICqrsBuilder builder)
            where T : IMemoryCachingBuilder
        {
            return AddMemoryCachingForHandlers<T>(builder, x => { });
        }

        public static ICqrsBuilder AddMemoryCachingForHandlers<T>(this ICqrsBuilder builder, Action<T> actions)
            where T : IMemoryCachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));
            var cachingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(cachingConfig);
            return builder;
        }

        public static ICqrsBuilder AddDistributedCachingForHandlers<T>(this ICqrsBuilder builder)
            where T : IDistributedCachingBuilder
        {
            return AddDistributedCachingForHandlers<T>(builder, x => { });
        }

        public static ICqrsBuilder AddDistributedCachingForHandlers<T>(this ICqrsBuilder builder, Action<T> actions)
            where T : IDistributedCachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));
            var cachingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(cachingConfig);
            return builder;
        }
    }
}
