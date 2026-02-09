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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="Type"/> including human-readable type name formatting,
    /// cache key generation, constructor inspection, and assignability checks.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets a human-readable generic type name (e.g., "List&lt;String&gt;" instead of "List`1").
        /// For non-generic types, returns <see cref="Type.Name"/>.
        /// </summary>
        /// <param name="type">The type to get the name for.</param>
        /// <returns>A formatted type name string.</returns>
        public static string GetGenericTypeName(this Type type)
        {
            var typeName = string.Empty;

            if (type.IsGenericType)
            {
                var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
                typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
            }
            else
            {
                typeName = type.Name;
            }

            return typeName;
        }

        /// <summary>
        /// Thread-safe cache for pretty-printed type names to avoid recomputation.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, string> PrettyPrintCache = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Returns a human-readable representation of the type, including nested generic arguments.
        /// Results are cached for performance.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <returns>A pretty-printed type name string.</returns>
        public static string PrettyPrint(this Type type)
        {
            return PrettyPrintCache.GetOrAdd(
                type,
                t =>
                {
                    try
                    {
                        return PrettyPrintRecursive(t, 0);
                    }
                    catch (Exception)
                    {
                        return t.Name;
                    }
                });
        }

        /// <summary>
        /// Thread-safe cache for type cache key strings.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, string> TypeCacheKeys = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Gets a unique cache key string for the type, combining its pretty-printed name with its hash code.
        /// Results are cached for performance.
        /// </summary>
        /// <param name="type">The type to generate a cache key for.</param>
        /// <returns>A cache key string in the format "TypeName[hash: hashCode]".</returns>
        public static string GetCacheKey(this Type type)
        {
            return TypeCacheKeys.GetOrAdd(
                type,
                t => $"{t.PrettyPrint()}[hash: {t.GetHashCode()}]");
        }

        /// <summary>
        /// Recursively builds a pretty-printed type name, expanding generic arguments up to a depth limit of 3
        /// to prevent infinite recursion with self-referencing generic types.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <param name="depth">The current recursion depth.</param>
        /// <returns>A formatted type name string.</returns>
        private static string PrettyPrintRecursive(Type type, int depth)
        {
            if (depth > 3)
            {
                return type.Name;
            }

            var nameParts = type.Name.Split('`');
            if (nameParts.Length == 1)
            {
                return nameParts[0];
            }

            var genericArguments = type.GetTypeInfo().GetGenericArguments();
            return !type.IsConstructedGenericType
                ? $"{nameParts[0]}<{new string(',', genericArguments.Length - 1)}>"
                : $"{nameParts[0]}<{string.Join(",", genericArguments.Select(t => PrettyPrintRecursive(t, depth + 1)))}>";
        }

        /// <summary>
        /// Determines whether the type has any constructor with a parameter matching the given predicate.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="predicate">A predicate to test each constructor parameter type against.</param>
        /// <returns><c>true</c> if any constructor parameter matches the predicate; otherwise, <c>false</c>.</returns>
        public static bool HasConstructorParameterOfType(this Type type, Predicate<Type> predicate)
        {
            return type.GetTypeInfo().GetConstructors()
                .Any(c => c.GetParameters()
                    .Any(p => predicate(p.ParameterType)));
        }

        /// <summary>
        /// Determines whether the type is assignable to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target type to check assignability against.</typeparam>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if instances of <paramref name="type"/> can be assigned to <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
        public static bool IsAssignableTo<T>(this Type type)
        {
            return typeof(T).GetTypeInfo().IsAssignableFrom(type);
        }
    }
}
