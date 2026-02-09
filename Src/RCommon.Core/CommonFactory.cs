
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RCommon
{
    /// <summary>
    /// Default implementation of <see cref="ICommonFactory{T}"/> that uses a <see cref="Func{TResult}"/>
    /// delegate to create instances of <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of object this factory creates.</typeparam>
    public class CommonFactory<TResult> : ICommonFactory<TResult>
    {
        private readonly Func<TResult> _initFunc;

        /// <summary>
        /// Initializes a new instance of <see cref="CommonFactory{TResult}"/> with the specified initialization function.
        /// </summary>
        /// <param name="initFunc">A delegate that creates new instances of <typeparamref name="TResult"/>.</param>
        public CommonFactory(Func<TResult> initFunc)
        {
            _initFunc = initFunc;
        }

        /// <inheritdoc />
        public TResult Create()
        {
            return _initFunc();
        }

        /// <inheritdoc />
        public TResult Create(Action<TResult> customize)
        {
            var concreteObject = _initFunc();
            customize(concreteObject);
            return concreteObject;
        }
    }

    /// <summary>
    /// Default implementation of <see cref="ICommonFactory{T, TResult}"/> that uses a <see cref="Func{T, TResult}"/>
    /// delegate to create instances of <typeparamref name="TResult"/> from a single argument.
    /// </summary>
    /// <typeparam name="T">The type of the input argument.</typeparam>
    /// <typeparam name="TResult">The type of object this factory creates.</typeparam>
    public class CommonFactory<T, TResult> : ICommonFactory<T, TResult>
    {
        private readonly Func<T, TResult> _initFunc;

        /// <summary>
        /// Initializes a new instance of <see cref="CommonFactory{T, TResult}"/> with the specified initialization function.
        /// </summary>
        /// <param name="initFunc">A delegate that creates new instances of <typeparamref name="TResult"/> from an argument of type <typeparamref name="T"/>.</param>
        public CommonFactory(Func<T, TResult> initFunc)
        {
            _initFunc = initFunc;
        }

        /// <inheritdoc />
        public TResult Create(T arg)
        {
            return _initFunc(arg);
        }

        /// <inheritdoc />
        public TResult Create(T arg, Action<TResult> customize)
        {
            var concreteObject = _initFunc(arg);
            customize(concreteObject);
            return concreteObject;
        }


    }

    /// <summary>
    /// Default implementation of <see cref="ICommonFactory{T, T2, TResult}"/> that uses a <see cref="Func{T, T2, TResult}"/>
    /// delegate to create instances of <typeparamref name="TResult"/> from two arguments.
    /// </summary>
    /// <typeparam name="T">The type of the first input argument.</typeparam>
    /// <typeparam name="T2">The type of the second input argument.</typeparam>
    /// <typeparam name="TResult">The type of object this factory creates.</typeparam>
    public class CommonFactory<T, T2, TResult> : ICommonFactory<T, T2, TResult>
    {
        private readonly Func<T, T2, TResult> _initFunc;

        /// <summary>
        /// Initializes a new instance of <see cref="CommonFactory{T, T2, TResult}"/> with the specified initialization function.
        /// </summary>
        /// <param name="initFunc">A delegate that creates new instances of <typeparamref name="TResult"/> from arguments of type <typeparamref name="T"/> and <typeparamref name="T2"/>.</param>
        public CommonFactory(Func<T, T2, TResult> initFunc)
        {
            _initFunc = initFunc;
        }

        /// <inheritdoc />
        public TResult Create(T arg, T2 arg2)
        {
            return _initFunc(arg, arg2);
        }

        /// <inheritdoc />
        public TResult Create(T arg, T2 arg2, Action<TResult> customize)
        {
            var concreteObject = _initFunc(arg, arg2);
            customize(concreteObject);
            return concreteObject;
        }


    }

}
