using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Reflection
{
    public static class ObjectGraphWalker
    {
        public static IEnumerable<T> TraverseGraphFor<T>(object root) where T : class
        {
            var results = new List<T>();
            var visited = new ArrayList();
            Walk(root, results, visited);
            return results.ToArray();
        }

        private static void Walk<T>(object source, IList<T> results, IList visited)
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

            // source is a sequence of objects
            if (source is IEnumerable)
            {
                // includes Array, IDictionary, IList, IQueryable
                WalkSequence((IEnumerable)source, results, visited);
            }

            // dive into the object's properties
            WalkComplexObject(source, results, visited);
        }

        private static void WalkSequence<T>(IEnumerable source,
            IList<T> results, IList visited)
            where T : class
        {
            if (source == null) return;
            foreach (var element in source)
            {
                Walk(element, results, visited);
            }
        }

        private static void WalkComplexObject<T>(object source,
            IList<T> results, IList visited)
            where T : class
        {
            if (source == null) return;
            var type = source.GetType();
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
