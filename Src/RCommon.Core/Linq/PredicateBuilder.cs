using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace RCommon.Linq
{
	/// <summary>
	/// See http://www.albahari.com/expressions for information and examples.
	/// </summary>
	/// <summary>
	/// Provides methods to dynamically compose LINQ predicate expressions using logical AND/OR operators.
	/// Start with <see cref="True{T}"/> or <see cref="False{T}"/> as an identity, then chain with
	/// <see cref="And{T}"/> or <see cref="Or{T}"/>.
	/// </summary>
	/// <remarks>
	/// See http://www.albahari.com/expressions for information and examples.
	/// </remarks>
	public static class PredicateBuilder
	{
		/// <summary>
		/// Creates a predicate expression that always evaluates to <c>true</c>.
		/// Use as the starting point when building AND-chained predicates.
		/// </summary>
		/// <typeparam name="T">The type the predicate operates on.</typeparam>
		/// <returns>An expression that always returns <c>true</c>.</returns>
		public static Expression<Func<T, bool>> True<T> () { return f => true; }

		/// <summary>
		/// Creates a predicate expression that always evaluates to <c>false</c>.
		/// Use as the starting point when building OR-chained predicates.
		/// </summary>
		/// <typeparam name="T">The type the predicate operates on.</typeparam>
		/// <returns>An expression that always returns <c>false</c>.</returns>
		public static Expression<Func<T, bool>> False<T> () { return f => false; }

		/// <summary>
		/// Combines two predicate expressions using a logical OR (short-circuit) operation.
		/// </summary>
		/// <typeparam name="T">The type the predicates operate on.</typeparam>
		/// <param name="expr1">The first predicate expression.</param>
		/// <param name="expr2">The second predicate expression to OR with the first.</param>
		/// <returns>A combined predicate expression representing <paramref name="expr1"/> OR <paramref name="expr2"/>.</returns>
		public static Expression<Func<T, bool>> Or<T> (this Expression<Func<T, bool>> expr1,
												  Expression<Func<T, bool>> expr2)
		{
			// Invoke expr2 using expr1's parameters so both share the same parameter expression
			var invokedExpr = Expression.Invoke (expr2, expr1.Parameters.Cast<Expression> ());
			return Expression.Lambda<Func<T, bool>>
				 (Expression.OrElse (expr1.Body, invokedExpr), expr1.Parameters);
		}

		/// <summary>
		/// Combines two predicate expressions using a logical AND (short-circuit) operation.
		/// </summary>
		/// <typeparam name="T">The type the predicates operate on.</typeparam>
		/// <param name="expr1">The first predicate expression.</param>
		/// <param name="expr2">The second predicate expression to AND with the first.</param>
		/// <returns>A combined predicate expression representing <paramref name="expr1"/> AND <paramref name="expr2"/>.</returns>
		public static Expression<Func<T, bool>> And<T> (this Expression<Func<T, bool>> expr1,
												   Expression<Func<T, bool>> expr2)
		{
			// Invoke expr2 using expr1's parameters so both share the same parameter expression
			var invokedExpr = Expression.Invoke (expr2, expr1.Parameters.Cast<Expression> ());
			return Expression.Lambda<Func<T, bool>>
				 (Expression.AndAlso (expr1.Body, invokedExpr), expr1.Parameters);
		}
	}

}
