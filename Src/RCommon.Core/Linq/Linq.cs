using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace RCommon.Linq
{
	/// <summary>
	/// Another good idea by Tomas Petricek.
	/// See http://tomasp.net/blog/dynamic-linq-queries.aspx for information on how it's used.
	/// </summary>
	public static class Linq
	{
		/// <summary>
		/// Returns the given anonymous method as a strongly-typed lambda expression.
		/// Useful for building dynamic LINQ queries where the compiler cannot infer the expression type.
		/// </summary>
		/// <typeparam name="T">The input type of the expression.</typeparam>
		/// <typeparam name="TResult">The return type of the expression.</typeparam>
		/// <param name="expr">The lambda expression to return.</param>
		/// <returns>The same expression, strongly typed.</returns>
		public static Expression<Func<T, TResult>> Expr<T, TResult> (Expression<Func<T, TResult>> expr)
		{
			return expr;
		}

		/// <summary>
		/// Returns the given anonymous function as a strongly-typed <see cref="Func{T, TResult}"/> delegate.
		/// Useful when the compiler cannot infer the delegate type from context.
		/// </summary>
		/// <typeparam name="T">The input type of the function.</typeparam>
		/// <typeparam name="TResult">The return type of the function.</typeparam>
		/// <param name="expr">The function delegate to return.</param>
		/// <returns>The same function, strongly typed.</returns>
		public static Func<T, TResult> Func<T, TResult> (Func<T, TResult> expr)
		{
			return expr;
		}
	}
}
