using RCommon.DataServices.Transactions;
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
    public static class DataServicesConfiguration
    {

        /// <summary>
        /// Configures RCommon unit of work settings.
        /// </summary>
        /// <typeparam name="T">A <see cref="IUnitOfWorkConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public static IRCommonConfiguration WithUnitOfWork<T>(this IRCommonConfiguration config) 
            where T : IUnitOfWorkConfiguration
        {
            var uowConfiguration = (T)Activator.CreateInstance(typeof(T), new object[] { config.Services });
            return config;
        }

        ///<summary>
        /// Configures RCommon unit of work settings.
        ///</summary>
        /// <typeparam name="T">A <see cref="IRCommonConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        ///<param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IUnitOfWorkConfiguration"/> instance.</param>
        ///<returns><see cref="IRCommonConfiguration"/></returns>
        public static IRCommonConfiguration WithUnitOfWork<T>(this IRCommonConfiguration config, Action<T> actions) 
            where T : IUnitOfWorkConfiguration
        {
            var uowConfiguration = (T)Activator.CreateInstance(typeof(T), new object[] { config.Services });
            actions(uowConfiguration);
            return config;
        }

    }
}
