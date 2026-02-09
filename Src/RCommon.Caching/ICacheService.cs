using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Caching
{
    /// <summary>
    /// Provides a uniform interface for cache read-through (get-or-create) operations,
    /// regardless of the underlying caching provider.
    /// </summary>
    /// <remarks>
    /// Implementations include <c>InMemoryCacheService</c>, <c>DistributedMemoryCacheService</c>,
    /// and <c>RedisCacheService</c>.
    /// </remarks>
    public interface ICacheService
    {
        /// <summary>
        /// Returns the cached value for the specified key, or creates, caches, and returns a new value
        /// using the provided factory delegate when no cached entry exists.
        /// </summary>
        /// <typeparam name="TData">The type of the cached data.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="data">A factory delegate invoked to produce the value when the key is not found in cache.</param>
        /// <returns>The cached or newly created value of type <typeparamref name="TData"/>.</returns>
        TData GetOrCreate<TData>(object key, Func<TData> data);

        /// <summary>
        /// Asynchronously returns the cached value for the specified key, or creates, caches, and returns
        /// a new value using the provided factory delegate when no cached entry exists.
        /// </summary>
        /// <typeparam name="TData">The type of the cached data.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="data">A factory delegate invoked to produce the value when the key is not found in cache.</param>
        /// <returns>A task representing the cached or newly created value of type <typeparamref name="TData"/>.</returns>
        Task<TData> GetOrCreateAsync<TData>(object key, Func<TData> data);
    }
}
