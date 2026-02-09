using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RCommon.Reflection
{
    /// <summary>
    /// Provides utility methods for reflection-based operations including generic type inspection,
    /// attribute retrieval, property path navigation, constant discovery, and compiled method invocation.
    /// </summary>
    public static class ReflectionHelper
    {
        //TODO: Ehhance summary
        /// <summary>
        /// Checks whether <paramref name="givenType"/> implements/inherits <paramref name="genericType"/>.
        /// </summary>
        /// <param name="givenType">Type to check</param>
        /// <param name="genericType">Generic type</param>
        public static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();

            if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }

            foreach (var interfaceType in givenTypeInfo.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }

            if (givenTypeInfo.BaseType == null)
            {
                return false;
            }

            return IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
        }

        /// <summary>
        /// Gets all closed generic types that <paramref name="givenType"/> implements or inherits
        /// matching the open generic type definition specified by <paramref name="genericType"/>.
        /// </summary>
        /// <param name="givenType">The type to inspect.</param>
        /// <param name="genericType">The open generic type definition to match against (e.g., <c>typeof(IRepository&lt;&gt;)</c>).</param>
        /// <returns>A list of closed generic types matching the specified definition.</returns>
        public static List<Type> GetImplementedGenericTypes(Type givenType, Type genericType)
        {
            var result = new List<Type>();
            AddImplementedGenericTypes(result, givenType, genericType);
            return result;
        }

        /// <summary>
        /// Recursively searches through a type's hierarchy (interfaces and base types)
        /// to find all closed generic types matching the given open generic type definition,
        /// adding each unique match to the result list.
        /// </summary>
        /// <param name="result">The accumulator list for matching types.</param>
        /// <param name="givenType">The current type being inspected.</param>
        /// <param name="genericType">The open generic type definition to match against.</param>
        private static void AddImplementedGenericTypes(List<Type> result, Type givenType, Type genericType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();

            if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                result.AddIfNotContains(givenType);
            }

            foreach (var interfaceType in givenTypeInfo.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    result.AddIfNotContains(interfaceType);
                }
            }

            if (givenTypeInfo.BaseType == null)
            {
                return;
            }

            AddImplementedGenericTypes(result, givenTypeInfo.BaseType, genericType);
        }

        /// <summary>
        /// Tries to gets an of attribute defined for a class member and it's declaring type including inherited attributes.
        /// Returns default value if it's not declared at all.
        /// </summary>
        /// <typeparam name="TAttribute">Type of the attribute</typeparam>
        /// <param name="memberInfo">MemberInfo</param>
        /// <param name="defaultValue">Default value (null as default)</param>
        /// <param name="inherit">Inherit attribute from base classes</param>
        public static TAttribute? GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute? defaultValue = default, bool inherit = true)
            where TAttribute : Attribute
        {
            //Get attribute on the member
            if (memberInfo.IsDefined(typeof(TAttribute), inherit))
            {
                return memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().First();
            }

            return defaultValue;
        }

        /// <summary>
        /// Tries to gets an of attribute defined for a class member and it's declaring type including inherited attributes.
        /// Returns default value if it's not declared at all.
        /// </summary>
        /// <typeparam name="TAttribute">Type of the attribute</typeparam>
        /// <param name="memberInfo">MemberInfo</param>
        /// <param name="defaultValue">Default value (null as default)</param>
        /// <param name="inherit">Inherit attribute from base classes</param>
        public static TAttribute? GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute? defaultValue = default, bool inherit = true)
            where TAttribute : class
        {
            return memberInfo.GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
                   ?? memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
                   ?? defaultValue;
        }

        /// <summary>
        /// Tries to gets attributes defined for a class member and it's declaring type including inherited attributes.
        /// </summary>
        /// <typeparam name="TAttribute">Type of the attribute</typeparam>
        /// <param name="memberInfo">MemberInfo</param>
        /// <param name="inherit">Inherit attribute from base classes</param>
        public static IEnumerable<TAttribute> GetAttributesOfMemberOrDeclaringType<TAttribute>(MemberInfo memberInfo, bool inherit = true)
            where TAttribute : class
        {
            var customAttributes = memberInfo.GetCustomAttributes(true).OfType<TAttribute>();
            var declaringTypeCustomAttributes =
                memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>();
            return declaringTypeCustomAttributes != null
                ? customAttributes.Concat(declaringTypeCustomAttributes).Distinct()
                : customAttributes;
        }

        /// <summary>
        /// Gets value of a property by it's full path from given object
        /// </summary>
        public static object? GetValueByPath(object obj, Type objectType, string propertyPath)
        {
            object? value = obj;
            var currentType = objectType;
            var objectPath = currentType.FullName;
            var absolutePropertyPath = propertyPath;
            if (objectPath != null && absolutePropertyPath.StartsWith(objectPath))
            {
                absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
            }

            foreach (var propertyName in absolutePropertyPath.Split('.'))
            {
                var property = currentType.GetProperty(propertyName);
                if (property != null)
                {
                    if (value != null)
                    {
                        value = property.GetValue(value, null);
                    }
                    currentType = property.PropertyType;
                }
                else
                {
                    value = null;
                    break;
                }
            }

            return value;
        }

        /// <summary>
        /// Sets value of a property by it's full path on given object
        /// </summary>
        internal static void SetValueByPath(object obj, Type objectType, string propertyPath, object value)
        {
            var currentType = objectType;
            PropertyInfo? property;
            var objectPath = currentType.FullName;
            var absolutePropertyPath = propertyPath;
            if (objectPath != null && absolutePropertyPath.StartsWith(objectPath))
            {
                absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
            }

            var properties = absolutePropertyPath.Split('.');

            if (properties.Length == 1)
            {
                property = objectType.GetProperty(properties.First());
                property?.SetValue(obj, value);
                return;
            }

            for (int i = 0; i < properties.Length - 1; i++)
            {
                property = currentType.GetProperty(properties[i]);
                if (property == null) return;
                obj = property.GetValue(obj, null)!;
                currentType = property.PropertyType;
            }

            property = currentType.GetProperty(properties.Last());
            property?.SetValue(obj, value);
        }


        /// <summary>
        /// Get all the constant values in the specified type (including the base type).
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string[] GetPublicConstantsRecursively(Type type)
        {
            const int maxRecursiveParameterValidationDepth = 8;

            var publicConstants = new List<string>();

            void Recursively(List<string> constants, Type targetType, int currentDepth)
            {
                if (currentDepth > maxRecursiveParameterValidationDepth)
                {
                    return;
                }

                constants.AddRange(targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(x => x.IsLiteral && !x.IsInitOnly)
                    .Select(x => x.GetValue(null)?.ToString() ?? string.Empty));

                var nestedTypes = targetType.GetNestedTypes(BindingFlags.Public);

                foreach (var nestedType in nestedTypes)
                {
                    Recursively(constants, nestedType, currentDepth + 1);
                }
            }

            Recursively(publicConstants, type, 1);

            return publicConstants.ToArray();
        }

        /// <summary>
        /// Handles correct upcast. If no upcast was needed, then this could be exchanged to an <c>Expression.Call</c>
        /// and an <c>Expression.Lambda</c>.
        /// </summary>
        public static TResult CompileMethodInvocation<TResult>(Type type, string methodName,
            params Type[] methodSignature)
        {
            var typeInfo = type.GetTypeInfo();
            var methods = typeInfo
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == methodName);

            var methodInfo = methodSignature == null || !methodSignature.Any()
                ? methods.SingleOrDefault()
                : methods.SingleOrDefault(m => m.GetParameters().Select(mp => mp.ParameterType).SequenceEqual(methodSignature));

            if (methodInfo == null)
            {
                throw new ArgumentException($"Type '{type.PrettyPrint()}' doesn't have a method called '{methodName}'");
            }

            return CompileMethodInvocation<TResult>(methodInfo);
        }

        /// <summary>
        /// Handles correct upcast. If no upcast was needed, then this could be exchanged to an <c>Expression.Call</c>
        /// and an <c>Expression.Lambda</c>.
        /// </summary>
        public static TResult CompileMethodInvocation<TResult>(MethodInfo methodInfo)
        {
            var genericArguments = typeof(TResult).GetTypeInfo().GetGenericArguments();
            var methodArgumentList = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            var funcArgumentList = genericArguments.Skip(1).Take(methodArgumentList.Count).ToList();

            if (funcArgumentList.Count != methodArgumentList.Count)
            {
                throw new ArgumentException("Incorrect number of arguments");
            }

            var instanceArgument = Expression.Parameter(genericArguments[0]);

            var argumentPairs = funcArgumentList.Zip(methodArgumentList, (s, d) => new { Source = s, Destination = d }).ToList();
            if (argumentPairs.All(a => a.Source == a.Destination))
            {
                // No need to do anything fancy, the types are the same
                var parameters = funcArgumentList.Select(Expression.Parameter).ToList();
                return Expression.Lambda<TResult>(Expression.Call(instanceArgument, methodInfo, parameters), new[] { instanceArgument }.Concat(parameters)).Compile();
            }

            var lambdaArgument = new List<ParameterExpression>
                {
                    instanceArgument,
                };

            var type = methodInfo.DeclaringType!;
            var instanceVariable = Expression.Variable(type);
            var blockVariables = new List<ParameterExpression>
                {
                        instanceVariable,
                };
            var blockExpressions = new List<Expression>
                {
                    Expression.Assign(instanceVariable, Expression.ConvertChecked(instanceArgument, type))
                };
            var callArguments = new List<ParameterExpression>();

            foreach (var a in argumentPairs)
            {
                if (a.Source == a.Destination)
                {
                    var sourceParameter = Expression.Parameter(a.Source);
                    lambdaArgument.Add(sourceParameter);
                    callArguments.Add(sourceParameter);
                }
                else
                {
                    var sourceParameter = Expression.Parameter(a.Source);
                    var destinationVariable = Expression.Variable(a.Destination);
                    var assignToDestination = Expression.Assign(destinationVariable, Expression.Convert(sourceParameter, a.Destination));

                    lambdaArgument.Add(sourceParameter);
                    callArguments.Add(destinationVariable);
                    blockVariables.Add(destinationVariable);
                    blockExpressions.Add(assignToDestination);
                }
            }

            var callExpression = Expression.Call(instanceVariable, methodInfo, callArguments);
            blockExpressions.Add(callExpression);

            var block = Expression.Block(blockVariables, blockExpressions);

            var lambdaExpression = Expression.Lambda<TResult>(block, lambdaArgument);

            return lambdaExpression.Compile();
        }
    }
}
