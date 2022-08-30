using RCommon.Configuration;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using RCommon.DataServices;

namespace RCommon
{
    public class DataServicesConfiguration : RCommonConfiguration, IDataServicesConfiguration
    {

        public DataServicesConfiguration(IContainerAdapter containerAdapter):base(containerAdapter)
        {

        }

        /// <summary>
        /// Configures <see cref="UnitOfWorkScope"/> settings.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance.</param>
        public override void Configure()
        {
            this.ContainerAdapter.AddScoped<IDataStoreProvider, DataStoreProvider>();

        }

        /// <summary>
        /// Configures RCommon unit of work settings.
        /// </summary>
        /// <typeparam name="T">A <see cref="IUnitOfWorkConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IUnitOfWorkConfiguration WithUnitOfWork<T>() where T : IUnitOfWorkConfiguration
        {
            var uowConfiguration = (T)Activator.CreateInstance(typeof(T), new object[] { this.ContainerAdapter });
            uowConfiguration.Configure();
            return uowConfiguration;
        }

        ///<summary>
        /// Configures RCommon unit of work settings.
        ///</summary>
        /// <typeparam name="T">A <see cref="IRCommonConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        ///<param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IUnitOfWorkConfiguration"/> instance.</param>
        ///<returns><see cref="IRCommonConfiguration"/></returns>
        public IUnitOfWorkConfiguration WithUnitOfWork<T>(Action<T> actions) where T : IUnitOfWorkConfiguration
        {
            var uowConfiguration = (T)Activator.CreateInstance(typeof(T), new object[] { this.ContainerAdapter });
            actions(uowConfiguration);
            uowConfiguration.Configure();
            return uowConfiguration;
        }

    }
}
