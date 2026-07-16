using System;
using System.Linq.Expressions;

namespace RCommon.Persistence.EFCore.Crud
{
    /// <summary>
    /// Extracts navigation property names from Include/ThenInclude expression bodies for use with
    /// EF Core's string-based <c>Include(string navigationPropertyPath)</c> overload.
    /// </summary>
    /// <remarks>
    /// EF Core's expression-based <c>Include</c>/<c>ThenInclude</c> rejects a <c>Convert(...)</c> node
    /// wrapping a collection navigation access (e.g. when a generic repository helper accepts
    /// <c>Expression&lt;Func&lt;TEntity, object&gt;&gt;</c> and the navigation type is boxed to
    /// <c>object</c>) with <c>"The expression '...' is invalid inside an 'Include' operation, since it
    /// does not represent a property access"</c> -- but only for collection navigations; reference
    /// navigations tolerate the same wrapping. String-based Include has no such limitation and works
    /// uniformly for both, so RCommon's generic repository Include/ThenInclude helpers extract the
    /// navigation name here and build the query with the string overload instead.
    /// </remarks>
    internal static class IncludeExpressionHelper
    {
        public static string GetNavigationPropertyName(Expression expression)
        {
            var body = expression;
            while (body is UnaryExpression unary &&
                   (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            {
                body = unary.Operand;
            }

            if (body is MemberExpression member)
            {
                return member.Member.Name;
            }

            throw new ArgumentException(
                $"Include/ThenInclude path must be a simple property access expression (e.g. 't => t.PropertyName'). Received: {expression}");
        }
    }
}
