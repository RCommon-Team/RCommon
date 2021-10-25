using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess
{
    public class ObjectAccessConfiguration : IObjectAccessConfiguration
    {
        private readonly IContainerAdapter _containerAdapter;

        public void Configure(IContainerAdapter containerAdapter)
        {
            _containerAdapter = containerAdapter;
        }

        /// <summary>
        /// Configure data providers (ORM only for now) used by RCommon.
        /// </summary>
        /// <typeparam name="T">A <see cref="IObjectAccessConfiguration"/> type that can be used to configure
        /// data providers for RCommon.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IObjectAccessConfiguration WithObjectAccess<T>() where T : IObjectAccessConfiguration, new()
        {
            var datConfiguration = (T)Activator.CreateInstance(typeof(T));
            datConfiguration.Configure(_containerAdapter);
            return this;
        }

        /// <summary>
        /// Configure data providers (ORM only for now) used by RCommon.
        /// </summary>
        /// <typeparam name="T">A <see cref="IObjectAccessConfiguration"/> type that can be used to configure
        /// data providers for RCommon.</typeparam>
        /// <param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IObjectAccessConfiguration"/> instance.</param>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IObjectAccessConfiguration WithObjectAccess<T>(Action<T> actions) where T : IObjectAccessConfiguration, new()
        {
            var dataConfiguration = (T)Activator.CreateInstance(typeof(T));
            actions(dataConfiguration);
            dataConfiguration.Configure(_containerAdapter);
            return this;
        }
    }
}
