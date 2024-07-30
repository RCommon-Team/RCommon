using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    public static class JsonBuilderExtensions
    {
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, x => { }, x => { }, x => { });
        }

        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<JsonSerializeOptions> serializeOptions,
            Action<JsonDeserializeOptions> deSerializeOptions)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, serializeOptions, deSerializeOptions, x => { });
        }

        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<JsonSerializeOptions> serializeOptions)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, serializeOptions, x => { }, x => { });
        }

        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder,
            Action<JsonDeserializeOptions> deSerializeOptions)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, x => { }, deSerializeOptions, x => { });
        }

        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IJsonBuilder
        {

            return WithJsonSerialization<T>(builder, x => { }, x => { }, actions);
        }

        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<JsonSerializeOptions> serializeOptions, 
            Action<JsonDeserializeOptions> deSerializeOptions, Action<T> actions)
            where T : IJsonBuilder
        {
            Guard.IsNotNull(serializeOptions, nameof(serializeOptions));
            Guard.IsNotNull(deSerializeOptions, nameof(deSerializeOptions));
            Guard.IsNotNull(actions, nameof(actions));
            builder.Services.Configure<JsonSerializeOptions>(serializeOptions);
            var jsonConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(jsonConfig);
            return builder;
        }
    }
}
