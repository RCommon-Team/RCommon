

using RCommon.DependencyInjection;
using System;

namespace RCommon.ObjectAccess
{
    /// <summary>
    /// Base interface implemented by specific data configurators that configure RCommon data providers.
    /// </summary>
    public interface IObjectAccessConfiguration
    {
        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that allows
        /// registering components.</param>
        void Configure(IContainerAdapter containerAdapter);

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