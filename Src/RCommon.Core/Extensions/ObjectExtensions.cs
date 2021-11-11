using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;

namespace RCommon.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Helper method to determine if two byte arrays are the same value even if they are different object references
        /// </summary>
        /// <param name="binaryValue1">This Object</param>
        /// <param name="binaryValue2">The Object you want to compare against</param>
        /// <returns>true if the two objects are equal</returns>
        public static bool BinaryEquals(this object binaryValue1, object binaryValue2)
        {
            if (Object.ReferenceEquals(binaryValue1, binaryValue2))
            {
                return true;
            }

            byte[] array1 = binaryValue1 as byte[];
            byte[] array2 = binaryValue2 as byte[];

            if (array1 != null && array2 != null)
            {
                if (array1.Length != array2.Length)
                {
                    return false;
                }

                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the value of a specified property from an object 
        /// using reflection.
        /// </summary>
        /// <param name="sourceObject">The source object from which the property is to be fetched</param>
        /// <param name="propertyName">The name of the property</param>
        /// <returns></returns>
        public static T GetPropertyValueWithReflection<T>(this object sourceObject,
          string propertyName)
        {
            Guard.Against<ArgumentNullException>(sourceObject == null, "sourceObject cannot be null");
            Guard.Against<ArgumentException>(string.IsNullOrEmpty(propertyName), "propertyName, cannot be null or empty");

            BindingFlags eFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo propertyInfo = sourceObject.GetType().GetProperty(propertyName, eFlags);

            if (propertyInfo == null)
            {
                throw new ApplicationException("Cannot find property [" + propertyName + "] in object type: " +
                                               sourceObject.GetType().Name);
            }

            return (T)propertyInfo.GetValue(sourceObject, null);
        }

        /// <summary>
        /// Sets the value of a specified property for an object 
        /// using reflection.
        /// </summary>
        /// <param name="anObject">the object whose property value will be set</param>
        /// <param name="propertyName">the name of the property</param>
        /// <param name="propertyValue">the value of the property</param>
        public static void SetPropertyValueWithReflection(this object anObject,
          string propertyName, object propertyValue)
        {
            Guard.Against<ArgumentNullException>(anObject == null, "anObject cannot be null");
            Guard.Against<ArgumentException>(string.IsNullOrEmpty(propertyName), "propertyName, cannot be null or empty");

            BindingFlags eFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo propertyInfo = anObject.GetType().GetProperty(propertyName, eFlags);

            Type dataType = propertyInfo.PropertyType;
            if (!(dataType.IsGenericType && (dataType.GetGenericTypeDefinition() == typeof(Nullable<>))))
            {
                if (propertyValue == null)
                {
                    throw new ArgumentNullException("propertyValue");
                }
            }
            if (propertyInfo == null)
                throw new ApplicationException("Cannot find property [" + propertyName + "] in object type: " +
                                               anObject.GetType().FullName);

            propertyInfo.SetValue(anObject, propertyValue, null);
        }

        ///<summary>
        /// Builds a key that from the full name of the type and the supplied user key.
        ///</summary>
        ///<param name="userKey">The user supplied key, if any.</param>
        ///<typeparam name="T">The type for which the key is built.</typeparam>
        ///<returns>string.</returns>
        public static string BuildFullKey<T>(this object userKey)
        {
            if (userKey == null)
                return typeof(T).FullName;
            return typeof(T).FullName + userKey;
        }

        /// <summary>
        /// Used to simplify and beautify casting an object to a type.
        /// </summary>
        /// <typeparam name="T">Type to be casted</typeparam>
        /// <param name="obj">Object to cast</param>
        /// <returns>Casted object</returns>
        public static T As<T>(this object obj)
            where T : class
        {
            return (T)obj;
        }

        /// <summary>
        /// Converts given object to a value type using <see cref="Convert.ChangeType(object,System.Type)"/> method.
        /// </summary>
        /// <param name="obj">Object to be converted</param>
        /// <typeparam name="T">Type of the target object</typeparam>
        /// <returns>Converted object</returns>
        public static T To<T>(this object obj)
            where T : struct
        {
            if (typeof(T) == typeof(Guid))
            {
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.ToString());
            }

            return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Check if an item is in a list.
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <param name="list">List of items</param>
        /// <typeparam name="T">Type of the items</typeparam>
        public static bool IsIn<T>(this T item, params T[] list)
        {
            return list.Contains(item);
        }

        /// <summary>
        /// Check if an item is in the given enumerable.
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <param name="items">Items</param>
        /// <typeparam name="T">Type of the items</typeparam>
        public static bool IsIn<T>(this T item, IEnumerable<T> items)
        {
            return items.Contains(item);
        }

        /// <summary>
        /// Can be used to conditionally perform a function
        /// on an object and return the modified or the original object.
        /// It is useful for chained calls.
        /// </summary>
        /// <param name="obj">An object</param>
        /// <param name="condition">A condition</param>
        /// <param name="func">A function that is executed only if the condition is <code>true</code></param>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <returns>
        /// Returns the modified object (by the <paramref name="func"/> if the <paramref name="condition"/> is <code>true</code>)
        /// or the original object if the <paramref name="condition"/> is <code>false</code>
        /// </returns>
        public static T If<T>(this T obj, bool condition, Func<T, T> func)
        {
            if (condition)
            {
                return func(obj);
            }

            return obj;
        }

        /// <summary>
        /// Can be used to conditionally perform an action
        /// on an object and return the original object.
        /// It is useful for chained calls on the object.
        /// </summary>
        /// <param name="obj">An object</param>
        /// <param name="condition">A condition</param>
        /// <param name="action">An action that is executed only if the condition is <code>true</code></param>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <returns>
        /// Returns the original object.
        /// </returns>
        public static T If<T>(this T obj, bool condition, Action<T> action)
        {
            if (condition)
            {
                action(obj);
            }

            return obj;
        }

        public static string GetGenericTypeName(this object @object)
        {
            return @object.GetType().GetGenericTypeName();
        }
    }
}
