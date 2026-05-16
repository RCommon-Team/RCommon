using System;

namespace RCommon.Bootstrapping
{
    /// <summary>
    /// Internal helpers for singleton-style WithX verbs that live outside RCommon.Core.
    /// Not intended for use by application code.
    /// </summary>
    public static class RCommonBuilderInternals
    {
        /// <summary>
        /// Returns the cached sub-builder concrete type that implements <typeparamref name="TInterface"/>, or null if none.
        /// </summary>
        public static Type? FindCachedImplementationOf<TInterface>(IRCommonBuilder builder)
        {
            return (builder as RCommonBuilder)?.TryGetCachedSubBuilderImplementing(typeof(TInterface));
        }
    }
}
