using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    public static class CachingBuilderExtensions
    {
        public static IRCommonBuilder WithCaching<T>(this IRCommonBuilder builder)
            where T : ICachingBuilder
        {
            return WithCaching<T>(builder, x => { });
        }

        public static IRCommonBuilder WithCaching<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : ICachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));

            // Event Handling Configurations 
            var cachingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(cachingConfig);
            return builder;
        }
    }
}
