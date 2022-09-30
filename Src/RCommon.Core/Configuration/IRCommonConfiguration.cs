using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon
{
    /// <summary>
    /// Configuration interface exposed by RCommon to configure different services exposed by RCommon.
    /// </summary>
    public interface IRCommonConfiguration
    {
        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithStateStorage<T>() where T : IStateStorageConfiguration;

        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IStateStorageConfiguration"/> instance.</param>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithStateStorage<T>(Action<T> actions) where T : IStateStorageConfiguration;

        IRCommonConfiguration WithGuidGenerator<T>(Action<SequentialGuidGeneratorOptions> actions) where T : IGuidGenerator;

        IRCommonConfiguration WithGuidGenerator<T>() where T : IGuidGenerator;

        IRCommonConfiguration WithDateTimeSystem<T>(Action<SystemTimeOptions> actions) where T : ISystemTime;
    }
}
