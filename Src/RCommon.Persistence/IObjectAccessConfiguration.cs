using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;

namespace RCommon
{
    /// <summary>
    /// Base interface implemented by specific data configurators that configure RCommon data providers.
    /// </summary>
    public interface IObjectAccessConfiguration : IRCommonConfiguration
    {
        IObjectAccessConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options);
    }
}
