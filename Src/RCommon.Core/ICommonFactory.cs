using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    /// <summary>
    /// Defines a factory that creates instances of <typeparamref name="T"/> with optional customization.
    /// </summary>
    /// <typeparam name="T">The type of object to create.</typeparam>
    /// <seealso cref="CommonFactory{TResult}"/>
    public interface ICommonFactory<T>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="T"/>.
        /// </summary>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        T Create();

        /// <summary>
        /// Creates a new instance of <typeparamref name="T"/> and applies the specified customization action.
        /// </summary>
        /// <param name="customize">An action to customize the created instance before it is returned.</param>
        /// <returns>A customized instance of <typeparamref name="T"/>.</returns>
        T Create(Action<T> customize);
    }

    /// <summary>
    /// Defines a factory that creates instances of <typeparamref name="TResult"/> using a single argument,
    /// with optional customization.
    /// </summary>
    /// <typeparam name="T">The type of the input argument.</typeparam>
    /// <typeparam name="TResult">The type of object to create.</typeparam>
    /// <seealso cref="CommonFactory{T, TResult}"/>
    public interface ICommonFactory<T, TResult>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified argument.
        /// </summary>
        /// <param name="arg">The input argument used during creation.</param>
        /// <returns>A new instance of <typeparamref name="TResult"/>.</returns>
        TResult Create(T arg);

        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified argument
        /// and applies the customization action.
        /// </summary>
        /// <param name="arg">The input argument used during creation.</param>
        /// <param name="customize">An action to customize the created instance before it is returned.</param>
        /// <returns>A customized instance of <typeparamref name="TResult"/>.</returns>
        TResult Create(T arg, Action<TResult> customize);
    }

    /// <summary>
    /// Defines a factory that creates instances of <typeparamref name="TResult"/> using two arguments,
    /// with optional customization.
    /// </summary>
    /// <typeparam name="T">The type of the first input argument.</typeparam>
    /// <typeparam name="T2">The type of the second input argument.</typeparam>
    /// <typeparam name="TResult">The type of object to create.</typeparam>
    /// <seealso cref="CommonFactory{T, T2, TResult}"/>
    public interface ICommonFactory<T, T2, TResult>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified arguments.
        /// </summary>
        /// <param name="arg">The first input argument.</param>
        /// <param name="arg2">The second input argument.</param>
        /// <returns>A new instance of <typeparamref name="TResult"/>.</returns>
        TResult Create(T arg, T2 arg2);

        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified arguments
        /// and applies the customization action.
        /// </summary>
        /// <param name="arg">The first input argument.</param>
        /// <param name="arg2">The second input argument.</param>
        /// <param name="customize">An action to customize the created instance before it is returned.</param>
        /// <returns>A customized instance of <typeparamref name="TResult"/>.</returns>
        TResult Create(T arg, T2 arg2, Action<TResult> customize);
    }

}
