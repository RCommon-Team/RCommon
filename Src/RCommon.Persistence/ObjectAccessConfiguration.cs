using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public static class ObjectAccessConfiguration
    {

        public static IObjectAccessConfiguration WithObjectAccess<T>(this IRCommonConfiguration config) where T: IObjectAccessConfiguration, new()
        {
            var dataConfiguration = (T)Activator.CreateInstance(typeof(T));
            config.ContainerAdapter.AddTransient(typeof(IObjectAccessConfiguration), dataConfiguration.GetType());
            dataConfiguration.Configure();
            return dataConfiguration;
        }

        public static IObjectAccessConfiguration WithObjectAccess<T>(this IRCommonConfiguration config, Action<T> actions) 
            where T : IObjectAccessConfiguration, new()
        {
            var dataConfiguration = (T)Activator.CreateInstance(typeof(T));
            config.ContainerAdapter.AddTransient(typeof(IObjectAccessConfiguration), dataConfiguration.GetType());
            actions(dataConfiguration);
            dataConfiguration.Configure();
            return dataConfiguration;
        }


    }
}
