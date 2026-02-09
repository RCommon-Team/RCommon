using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using RCommon.Linq;
using RCommon.Collections;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="IQueryable{T}"/> including dynamic ordering,
    /// conditional filtering, LIKE-style queries, and pagination.
    /// </summary>
    public static class IQueryableExtensions
    {

        /// <summary>
        /// Dynamically orders an <see cref="IQueryable{TEntity}"/> by a property name specified as a string.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="source">The queryable source.</param>
        /// <param name="orderByProperty">The name of the property to order by.</param>
        /// <param name="desc">If <c>true</c>, orders descending; otherwise, ascending.</param>
        /// <returns>An ordered <see cref="IQueryable{TEntity}"/>.</returns>
        /// <remarks>
        /// Builds an expression tree at runtime to call <c>OrderBy</c> or <c>OrderByDescending</c>
        /// on the query provider using the specified property name.
        /// </remarks>
        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty,
                     bool desc) where TEntity : class
        {
            // Determine which Queryable method to call based on sort direction
            string command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(TEntity);
            var property = type.GetProperty(orderByProperty)
                ?? throw new ArgumentException($"Property '{orderByProperty}' not found on type '{type.FullName}'.", nameof(orderByProperty));
            var parameter = Expression.Parameter(type, "p");
            // Build a member access expression for the property (e.g., p.PropertyName)
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            // Create a method call expression to Queryable.OrderBy/OrderByDescending
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
                                   source.Expression, Expression.Quote(orderByExpression));
            return source.Provider.CreateQuery<TEntity>(resultExpression);

        }

        

        /// <summary>
        /// Conditionally filters an <see cref="IQueryable{TSource}"/> based on a boolean condition.
        /// If the condition is false, the source is returned unfiltered.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source.</typeparam>
        /// <param name="source">The queryable source to filter.</param>
        /// <param name="condition">If <c>true</c>, the predicate is applied; otherwise, the source is returned as-is.</param>
        /// <param name="predicate">The filter expression to apply when <paramref name="condition"/> is <c>true</c>.</param>
        /// <returns>A filtered or unfiltered queryable depending on the condition.</returns>
        public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source, bool condition, Expression<Func<TSource, bool>> predicate)
        {
            if (condition)
                return source.Where(predicate);
            else
                return source;
        }

        /// <summary>
        /// Conditionally filters an <see cref="IQueryable{TSource}"/> based on a boolean condition, using an index-aware predicate.
        /// If the condition is false, the source is returned unfiltered.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source.</typeparam>
        /// <param name="source">The queryable source to filter.</param>
        /// <param name="condition">If <c>true</c>, the predicate is applied; otherwise, the source is returned as-is.</param>
        /// <param name="predicate">The index-aware filter expression to apply when <paramref name="condition"/> is <c>true</c>.</param>
        /// <returns>A filtered or unfiltered queryable depending on the condition.</returns>
        public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source, bool condition, Expression<Func<TSource, int, bool>> predicate)
        {
            if (condition)
                return source.Where(predicate);
            else
                return source;
        }

        /// <summary>
        /// Filters an <see cref="IQueryable{TSource}"/> using SQL LIKE-style matching with the <c>%</c> wildcard character.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source.</typeparam>
        /// <param name="source">The queryable source to filter.</param>
        /// <param name="valueSelector">An expression selecting the string property to match against.</param>
        /// <param name="value">The pattern to match, using <c>%</c> as the wildcard character.</param>
        /// <returns>A filtered <see cref="IQueryable{TSource}"/>.</returns>
        public static IQueryable<TSource> WhereLike<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, string>> valueSelector, string value)
        {
            return source.Where(BuildLikeExpression(valueSelector, value, '%'));
        }

        /// <summary>
        /// Builds an expression tree that mimics SQL LIKE behavior by mapping wildcard positions
        /// to <see cref="string.Contains(string)"/>, <see cref="string.StartsWith(string)"/>, or <see cref="string.EndsWith(string)"/>.
        /// </summary>
        /// <typeparam name="TElement">The type of element in the source.</typeparam>
        /// <param name="valueSelector">An expression selecting the string property to match against.</param>
        /// <param name="value">The pattern to match, with wildcards.</param>
        /// <param name="wildcard">The wildcard character (typically <c>%</c>).</param>
        /// <returns>A predicate expression representing the LIKE comparison.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="valueSelector"/> is null.</exception>
        public static Expression<Func<TElement, bool>> BuildLikeExpression<TElement>(Expression<Func<TElement, string>> valueSelector, string value, char wildcard)
        {
            if (valueSelector == null)
                throw new ArgumentNullException("valueSelector");

            var method = GetLikeMethod(value, wildcard);

            value = value.Trim(wildcard);
            var body = Expression.Call(valueSelector.Body, method, Expression.Constant(value));

            var parameter = valueSelector.Parameters.Single();
            return Expression.Lambda<Func<TElement, bool>>(body, parameter);
        }

        /// <summary>
        /// Determines the appropriate <see cref="string"/> method (<c>Contains</c>, <c>StartsWith</c>, or <c>EndsWith</c>)
        /// based on the position of wildcard characters in the value.
        /// </summary>
        /// <param name="value">The pattern string containing wildcards.</param>
        /// <param name="wildcard">The wildcard character to detect.</param>
        /// <returns>The <see cref="MethodInfo"/> for the chosen string comparison method.</returns>
        private static MethodInfo GetLikeMethod(string value, char wildcard)
        {
            var methodName = "Contains";

            var textLength = value.Length;
            value = value.TrimEnd(wildcard);
            if (textLength > value.Length)
            {
                methodName = "StartsWith";
                textLength = value.Length;
            }

            value = value.TrimStart(wildcard);
            if (textLength > value.Length)
            {
                //methodName = (methodName == "StartsWith") ? "Contains" : "EndsWith";//IF Business changes their mind to make it similar to LIKE function then uncomment this line.
                methodName = "Contains";
                textLength = value.Length;
            }

            var stringType = typeof(string);
            return stringType.GetMethod(methodName, new Type[] { stringType })
                ?? throw new InvalidOperationException($"Method '{methodName}' not found on type 'System.String'.");
        }

        /// <summary>
        /// Converts an <see cref="IQueryable{T}"/> to an <see cref="IPaginatedList{T}"/> with the specified page number and size.
        /// </summary>
        /// <typeparam name="T">The type of elements in the queryable.</typeparam>
        /// <param name="source">The queryable source to paginate.</param>
        /// <param name="pageNumber">The 1-based page number. Defaults to 1.</param>
        /// <param name="pageSize">The number of items per page. Defaults to 10. Must be greater than zero.</param>
        /// <returns>A paginated list containing the items for the requested page.</returns>
        /// <seealso cref="PaginatedList{T}"/>
        public static IPaginatedList<T> ToPaginatedList<T>(this IQueryable<T> source, int pageNumber = 1, int pageSize = 10)
        {
            Guard.IsNotNegativeOrZero(pageSize, "pageSize");

            return new PaginatedList<T>(source, pageNumber, pageSize);
        }

    }
}
