

using RCommon.DependencyInjection;

namespace RCommon.Configuration
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
    }
}