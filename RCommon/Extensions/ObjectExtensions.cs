using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RCommon.Validation;

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
            return EqualityComparer.BinaryEquals(binaryValue1, binaryValue2);
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
    }
}
