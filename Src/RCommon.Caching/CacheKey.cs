// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace RCommon.Caching
{
    /// <summary>
    /// Represents a strongly-typed cache key with built-in validation and factory methods.
    /// </summary>
    /// <remarks>
    /// Cache keys are validated on construction to ensure they are non-empty and do not exceed
    /// <see cref="MaxLength"/> characters. Use the static <see cref="With(string[])"/> factory
    /// methods to create keys from component parts.
    /// </remarks>
    public class CacheKey
    {
        /// <summary>
        /// The maximum allowed length for a cache key value.
        /// </summary>
        public const int MaxLength = 256;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheKey"/> class.
        /// </summary>
        /// <param name="value">The cache key string value.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> exceeds <see cref="MaxLength"/>.</exception>
        public CacheKey(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));
            if (value.Length > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Cache keys can maximum be '{MaxLength}' in length");
        }

        /// <summary>
        /// Creates a <see cref="CacheKey"/> by joining the specified key segments with a hyphen delimiter.
        /// </summary>
        /// <param name="keys">The key segments to join.</param>
        /// <returns>A new <see cref="CacheKey"/> composed of the joined segments.</returns>
        public static CacheKey With(params string[] keys)
        {
            return new CacheKey(string.Join("-", keys));
        }

        /// <summary>
        /// Creates a <see cref="CacheKey"/> scoped to the specified owner type, using its cache key
        /// representation as a prefix followed by the joined key segments.
        /// </summary>
        /// <param name="ownerType">The type used to scope the cache key.</param>
        /// <param name="keys">The key segments to join after the type prefix.</param>
        /// <returns>A new <see cref="CacheKey"/> in the format <c>TypeCacheKey:segment1-segment2</c>.</returns>
        public static CacheKey With(Type ownerType, params string[] keys)
        {
            return With($"{ownerType.GetCacheKey()}:{string.Join("-", keys)}");
        }
    }
}
