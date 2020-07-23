using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public interface ICommonFactory<T>
    {
        T Create();
        T Create(Action<T> customize);
    }

    public interface ICommonFactory<T, TResult>
    {
        TResult Create(T arg);
        TResult Create(T arg, Action<TResult> customize);
    }

}
