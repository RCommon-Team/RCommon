using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Reflection
{
    /// <summary>
    /// Recursively traverses an object graph, searching for instances of a specified type
    /// across all public properties, fields, and enumerable collections.
    /// </summary>
    /// <remarks>
    /// The walker tracks visited objects to avoid infinite recursion from circular references.
    /// </remarks>
    public static class ObjectGraphWalker
    {
        /// <summary>
        /// Traverses an object graph starting from <paramref name="root"/> and collects all instances
        /// of type <typeparamref name="T"/> found within the graph.
        /// </summary>
        /// <typeparam name="T">The type to search for in the object graph. Must be a reference type.</typeparam>
        /// <param name="root">The root object to begin traversal from.</param>
        /// <returns>An enumerable of all <typeparamref name="T"/> instances found in the graph.</returns>
        public static IEnumerable<T> TraverseGraphFor<T>(object root) where T : class
        {
            var results = new List<T>();
            var visited = new ArrayList();
            Walk(root, results, visited);
            return results.ToArray();
        }

        /// <summary>
        /// Recursively walks an object: checks if it matches <typeparamref name="T"/>,
        /// enumerates it if it is a sequence, and inspects its public properties and fields.
        /// Tracks visited objects to prevent infinite loops from circular references.
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="source">The current object being inspected.</param>
        /// <param name="results">The accumulator list for matching instances.</param>
        /// <param name="visited">The list of already-visited objects to prevent cycles.</param>
        private static void Walk<T>(object? source, IList<T> results, IList visited)
            where T : class
        {
            if (source == null) return;
            if (visited.Contains(source)) return;
            visited.Add(source);

            // source is instance of T or any derived class
            if (typeof(T).IsInstanceOfType(source))
            {
                results.Add((T)source);
            }

            // source is a sequence of objects (includes Array, IDictionary, IList, IQueryable)
            if (source is IEnumerable)
            {
                WalkSequence((IEnumerable)source, results, visited);
            }

            // dive into the object's properties and fields
            WalkComplexObject(source, results, visited);
        }

        /// <summary>
        /// Iterates over each element in a sequence and recursively walks it.
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="source">The enumerable sequence to iterate.</param>
        /// <param name="results">The accumulator list for matching instances.</param>
        /// <param name="visited">The list of already-visited objects to prevent cycles.</param>
        private static void WalkSequence<T>(IEnumerable? source,
            IList<T> results, IList visited)
            where T : class
        {
            if (source == null) return;
            foreach (var element in source)
            {
                Walk(element, results, visited);
            }
        }

        /// <summary>
        /// Inspects all public instance fields and readable non-indexed properties of the source object,
        /// recursively walking each value to find instances of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="source">The complex object whose members are inspected.</param>
        /// <param name="results">The accumulator list for matching instances.</param>
        /// <param name="visited">The list of already-visited objects to prevent cycles.</param>
        private static void WalkComplexObject<T>(object? source,
            IList<T> results, IList visited)
            where T : class
        {
            if (source == null) return;
            var type = source.GetType();
            // Only inspect readable, non-indexed properties to avoid exceptions
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && !x.GetIndexParameters().Any());
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            // search its public fields and properties
            foreach (var field in fields)
            {
                Walk(field.GetValue(source), results, visited);
            }
            foreach (var property in properties)
            {
                Walk(property.GetValue(source), results, visited);
            }
        }
    }
}
