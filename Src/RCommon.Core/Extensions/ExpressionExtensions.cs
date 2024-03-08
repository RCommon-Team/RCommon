using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace RCommon
{
    public static class ExpressionExtensions
    {

        public static TResult Invoke<TResult>(this Expression<Func<TResult>> expr)
        {
            return expr.Compile().Invoke();
        }

        public static TResult Invoke<T1, TResult>(this Expression<Func<T1, TResult>> expr, T1 arg1)
        {
            return expr.Compile().Invoke(arg1);
        }

        public static TResult Invoke<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> expr, T1 arg1, T2 arg2)
        {
            return expr.Compile().Invoke(arg1, arg2);
        }

        public static TResult Invoke<T1, T2, T3, TResult>(
            this Expression<Func<T1, T2, T3, TResult>> expr, T1 arg1, T2 arg2, T3 arg3)
        {
            return expr.Compile().Invoke(arg1, arg2, arg3);
        }

        public static TResult Invoke<T1, T2, T3, T4, TResult>(
            this Expression<Func<T1, T2, T3, T4, TResult>> expr, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return expr.Compile().Invoke(arg1, arg2, arg3, arg4);
        }
    }
}
