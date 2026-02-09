using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods to compile and invoke <see cref="Expression{TDelegate}"/> trees in a single call.
    /// </summary>
    /// <remarks>
    /// Each invocation compiles the expression tree, which can be expensive. Consider caching the compiled
    /// delegate if performance is critical.
    /// </remarks>
    public static class ExpressionExtensions
    {

        /// <summary>
        /// Compiles and invokes a parameterless expression.
        /// </summary>
        /// <typeparam name="TResult">The return type of the expression.</typeparam>
        /// <param name="expr">The expression to compile and invoke.</param>
        /// <returns>The result of invoking the compiled expression.</returns>
        public static TResult Invoke<TResult>(this Expression<Func<TResult>> expr)
        {
            return expr.Compile().Invoke();
        }

        /// <summary>
        /// Compiles and invokes an expression with one argument.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="TResult">The return type of the expression.</typeparam>
        /// <param name="expr">The expression to compile and invoke.</param>
        /// <param name="arg1">The first argument.</param>
        /// <returns>The result of invoking the compiled expression.</returns>
        public static TResult Invoke<T1, TResult>(this Expression<Func<T1, TResult>> expr, T1 arg1)
        {
            return expr.Compile().Invoke(arg1);
        }

        /// <summary>
        /// Compiles and invokes an expression with two arguments.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="TResult">The return type of the expression.</typeparam>
        /// <param name="expr">The expression to compile and invoke.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The result of invoking the compiled expression.</returns>
        public static TResult Invoke<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> expr, T1 arg1, T2 arg2)
        {
            return expr.Compile().Invoke(arg1, arg2);
        }

        /// <summary>
        /// Compiles and invokes an expression with three arguments.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <typeparam name="TResult">The return type of the expression.</typeparam>
        /// <param name="expr">The expression to compile and invoke.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The result of invoking the compiled expression.</returns>
        public static TResult Invoke<T1, T2, T3, TResult>(
            this Expression<Func<T1, T2, T3, TResult>> expr, T1 arg1, T2 arg2, T3 arg3)
        {
            return expr.Compile().Invoke(arg1, arg2, arg3);
        }

        /// <summary>
        /// Compiles and invokes an expression with four arguments.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <typeparam name="T4">The type of the fourth argument.</typeparam>
        /// <typeparam name="TResult">The return type of the expression.</typeparam>
        /// <param name="expr">The expression to compile and invoke.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <returns>The result of invoking the compiled expression.</returns>
        public static TResult Invoke<T1, T2, T3, T4, TResult>(
            this Expression<Func<T1, T2, T3, T4, TResult>> expr, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return expr.Compile().Invoke(arg1, arg2, arg3, arg4);
        }
    }
}
