using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    /// <summary>
    /// Provides extension methods on <see cref="IRCommonBuilder"/> for registering JSON serialization
    /// into the RCommon configuration pipeline.
    /// </summary>
    /// <remarks>
    /// All overloads delegate to the primary overload that accepts serialize options, deserialize options,
    /// and a builder configuration action. Overloads that omit parameters supply no-op defaults.
    /// </remarks>
    public static class JsonBuilderExtensions
    {
        /// <summary>
        /// Registers JSON serialization using the specified <typeparamref name="T"/> builder with default options.
        /// </summary>
        /// <typeparam name="T">An <see cref="IJsonBuilder"/> implementation such as
        /// <c>JsonNetBuilder</c> or <c>TextJsonBuilder</c>.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, x => { }, x => { }, x => { });
        }

        /// <summary>
        /// Registers JSON serialization with custom serialize and deserialize options.
        /// </summary>
        /// <typeparam name="T">An <see cref="IJsonBuilder"/> implementation.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="serializeOptions">An action to configure <see cref="JsonSerializeOptions"/>.</param>
        /// <param name="deSerializeOptions">An action to configure <see cref="JsonDeserializeOptions"/>.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<JsonSerializeOptions> serializeOptions,
            Action<JsonDeserializeOptions> deSerializeOptions)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, serializeOptions, deSerializeOptions, x => { });
        }

        /// <summary>
        /// Registers JSON serialization with custom serialize options only.
        /// </summary>
        /// <typeparam name="T">An <see cref="IJsonBuilder"/> implementation.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="serializeOptions">An action to configure <see cref="JsonSerializeOptions"/>.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<JsonSerializeOptions> serializeOptions)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, serializeOptions, x => { }, x => { });
        }

        /// <summary>
        /// Registers JSON serialization with custom deserialize options only.
        /// </summary>
        /// <typeparam name="T">An <see cref="IJsonBuilder"/> implementation.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="deSerializeOptions">An action to configure <see cref="JsonDeserializeOptions"/>.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder,
            Action<JsonDeserializeOptions> deSerializeOptions)
            where T : IJsonBuilder
        {
            return WithJsonSerialization<T>(builder, x => { }, deSerializeOptions, x => { });
        }

        /// <summary>
        /// Registers JSON serialization with a custom builder configuration action.
        /// </summary>
        /// <typeparam name="T">An <see cref="IJsonBuilder"/> implementation.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">An action to further configure the <typeparamref name="T"/> builder instance.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IJsonBuilder
        {

            return WithJsonSerialization<T>(builder, x => { }, x => { }, actions);
        }

        /// <summary>
        /// Primary overload that registers JSON serialization with full control over serialize options,
        /// deserialize options, and builder-specific configuration.
        /// </summary>
        /// <typeparam name="T">An <see cref="IJsonBuilder"/> implementation.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="serializeOptions">An action to configure <see cref="JsonSerializeOptions"/>.</param>
        /// <param name="deSerializeOptions">An action to configure <see cref="JsonDeserializeOptions"/>.</param>
        /// <param name="actions">An action to further configure the <typeparamref name="T"/> builder instance.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        /// <remarks>
        /// This method uses <see cref="Activator.CreateInstance(Type, object[])"/> to instantiate the builder,
        /// passing the <see cref="IRCommonBuilder"/> as the constructor argument. The builder's constructor
        /// is expected to register its serialization services into the DI container.
        /// </remarks>
        public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder, Action<JsonSerializeOptions> serializeOptions,
            Action<JsonDeserializeOptions> deSerializeOptions, Action<T> actions)
            where T : IJsonBuilder
        {
            Guard.IsNotNull(serializeOptions, nameof(serializeOptions));
            Guard.IsNotNull(deSerializeOptions, nameof(deSerializeOptions));
            Guard.IsNotNull(actions, nameof(actions));
            builder.Services.Configure<JsonSerializeOptions>(serializeOptions);

            // Instantiate the builder via reflection; the constructor registers serializer services into DI
            var jsonConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
            actions(jsonConfig);
            return builder;
        }
    }
}
