

using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;

namespace RCommon.Persistance
{
    /// <summary>
    /// Base interface implemented by specific data configurators that configure RCommon data providers.
    /// </summary>
    public interface IObjectAccessConfiguration : IServiceConfiguration
    {

        /// <summary>
        /// Configure data providers used by RCommon.
        /// </summary>
        /// <typeparam name="T">A <see cref="IObjectAccessConfiguration"/> type that can be used to configure
        /// data providers for RCommon.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IObjectAccessConfiguration WithObjectAccess<T>() where T : IObjectAccessConfiguration, new();

        /// <summary>
        /// Configure data providers used by RCommon.
        /// </summary>
        /// <typeparam name="T">A <see cref="IObjectAccessConfiguration"/> type that can be used to configure
        /// data providers for RCommon.</typeparam>
        /// <param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IObjectAccessConfiguration"/> instance.</param>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IObjectAccessConfiguration WithObjectAccess<T>(Action<T> actions) where T : IObjectAccessConfiguration, new();
    }
}