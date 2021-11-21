using RCommon.BusinessEntities;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public static class ObjectAccessConfigurationExtensions
    {

        public static IObjectAccessConfiguration WithObjectAccess<T>(this IRCommonConfiguration config) where T: IObjectAccessConfiguration
        {
            config = WithChangeTracking(config);
            var dataConfiguration = (T)Activator.CreateInstance(typeof(T), new object[] { config.ContainerAdapter });
            dataConfiguration.Configure();
            return dataConfiguration;
        }

        public static IObjectAccessConfiguration WithObjectAccess<T>(this IRCommonConfiguration config, Action<T> actions) 
            where T : IObjectAccessConfiguration
        {
            config = WithChangeTracking(config);
            var dataConfiguration = (T)Activator.CreateInstance(typeof(T), new object[] { config.ContainerAdapter });
            actions(dataConfiguration);
            dataConfiguration.Configure();
            return dataConfiguration;
        }

        /// <summary>
        /// Right now we are always using change tracking due to requirements for publishing entity events and those events being
        /// somewhat tied to Change Tracking.
        /// </summary>
        /// <param name="config">Instance of <see cref="IRCommonConfiguration"/>passed in.</param>
        /// <returns>Updated instance of <see cref="IRCommonConfiguration"/>RCommon Configuration</returns>
        private static IRCommonConfiguration WithChangeTracking(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.AddScoped<IChangeTracker, ChangeTracker>();
            return config;
        }

    }
}
