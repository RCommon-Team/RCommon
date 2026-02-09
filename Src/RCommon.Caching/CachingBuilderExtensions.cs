using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    /// <summary>
    /// Extension methods on <see cref="IRCommonBuilder"/> for registering caching infrastructure.
    /// </summary>
    public static class CachingBuilderExtensions
    {
        /// <summary>
        /// Registers an <see cref="IMemoryCachingBuilder"/> implementation with default configuration.
        /// </summary>
        /// <typeparam name="T">The concrete memory caching builder type to activate.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> for chaining.</returns>
        public static IRCommonBuilder WithMemoryCaching<T>(this IRCommonBuilder builder)
            where T : IMemoryCachingBuilder
        {
            return WithMemoryCaching<T>(builder, x => { });
        }

        /// <summary>
        /// Registers an <see cref="IMemoryCachingBuilder"/> implementation and applies the specified configuration actions.
        /// </summary>
        /// <typeparam name="T">The concrete memory caching builder type to activate.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">A delegate to configure the caching builder.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> for chaining.</returns>
        /// <remarks>
        /// The builder type <typeparamref name="T"/> is created via <see cref="Activator.CreateInstance(Type, object[])"/>
        /// and must have a constructor that accepts an <see cref="IRCommonBuilder"/>.
        /// </remarks>
        public static IRCommonBuilder WithMemoryCaching<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IMemoryCachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));
            // Create the builder via reflection, passing the IRCommonBuilder to its constructor
            var cachingConfig = (T)(Activator.CreateInstance(typeof(T), new object[] { builder })
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}."));
            actions(cachingConfig);
            return builder;
        }

        /// <summary>
        /// Registers an <see cref="IDistributedCachingBuilder"/> implementation with default configuration.
        /// </summary>
        /// <typeparam name="T">The concrete distributed caching builder type to activate.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> for chaining.</returns>
        public static IRCommonBuilder WithDistributedCaching<T>(this IRCommonBuilder builder)
            where T : IDistributedCachingBuilder
        {
            return WithDistributedCaching<T>(builder, x => { });
        }

        /// <summary>
        /// Registers an <see cref="IDistributedCachingBuilder"/> implementation and applies the specified configuration actions.
        /// </summary>
        /// <typeparam name="T">The concrete distributed caching builder type to activate.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">A delegate to configure the caching builder.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> for chaining.</returns>
        /// <remarks>
        /// The builder type <typeparamref name="T"/> is created via <see cref="Activator.CreateInstance(Type, object[])"/>
        /// and must have a constructor that accepts an <see cref="IRCommonBuilder"/>.
        /// </remarks>
        public static IRCommonBuilder WithDistributedCaching<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IDistributedCachingBuilder
        {
            Guard.IsNotNull(actions, nameof(actions));
            // Create the builder via reflection, passing the IRCommonBuilder to its constructor
            var cachingConfig = (T)(Activator.CreateInstance(typeof(T), new object[] { builder })
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}."));
            actions(cachingConfig);
            return builder;
        }

    }
}
