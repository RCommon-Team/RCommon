using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public class DataServicesConfiguration : IUnitOfWorkConfiguration
    {
        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.AddScoped<IDataStoreProvider, DataStoreProvider>();
        }

        /// <summary>
        /// Configures RCommon unit of work settings.
        /// </summary>
        /// <typeparam name="T">A <see cref="IUnitOfWorkConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IUnitOfWorkConfiguration WithUnitOfWork<T>() where T : IUnitOfWorkConfiguration, new()
        {
            var uowConfiguration = (T)Activator.CreateInstance(typeof(T));
            uowConfiguration.Configure(_containerAdapter);
            return this;
        }

        ///<summary>
        /// Configures RCommon unit of work settings.
        ///</summary>
        /// <typeparam name="T">A <see cref="IRCommonConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        ///<param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IUnitOfWorkConfiguration"/> instance.</param>
        ///<returns><see cref="IRCommonConfiguration"/></returns>
        public IUnitOfWorkConfiguration WithUnitOfWork<T>(Action<T> actions) where T : IUnitOfWorkConfiguration, new()
        {
            var uowConfiguration = (T)Activator.CreateInstance(typeof(T));
            actions(uowConfiguration);
            uowConfiguration.Configure(_containerAdapter);
            return this;
        }

    }
}
