using Microsoft.Extensions.DependencyInjection;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Persistence;
using RCommon.Persistence.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class PersistenceBuilderExtensions
    {

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder config) 
            where TObjectAccess: IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            return WithPersistence<TObjectAccess, TUnitOfWork>(config, x => { }, x => { });
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder config,
            Action<TObjectAccess> objectAccessActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {

            return WithPersistence<TObjectAccess, TUnitOfWork>(config, objectAccessActions, x => { });
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder config,
            Action<TUnitOfWork> uniOfWorkActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {

            return WithPersistence<TObjectAccess, TUnitOfWork>(config, x => { }, uniOfWorkActions);
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder config, 
            Action<TObjectAccess> objectAccessActions, Action<TUnitOfWork> unitOfWorkActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            // Data Store Management
            StaticDataStore.DataStores = (StaticDataStore.DataStores == null ? new System.Collections.Concurrent.ConcurrentDictionary<string, Type>() : StaticDataStore.DataStores);
            config.Services.AddSingleton<IDataStoreRegistry, StaticDataStoreRegistry>();

            // Object Access and Unit of Work Configurations 
            var dataConfiguration = (TObjectAccess)Activator.CreateInstance(typeof(TObjectAccess), new object[] { config.Services });
            objectAccessActions(dataConfiguration);
            var unitOfWorkConfiguration = (TUnitOfWork)Activator.CreateInstance(typeof(TUnitOfWork), new object[] { config.Services });
            unitOfWorkActions(unitOfWorkConfiguration);
            config = WithChangeTracking(config);
            return config;
        }


        /// <summary>
        /// Right now we are always using change tracking due to requirements for publishing entity events and those events being
        /// somewhat tied to Change Tracking.
        /// </summary>
        /// <param name="config">Instance of <see cref="IRCommonBuilder"/>passed in.</param>
        /// <returns>Updated instance of <see cref="IRCommonBuilder"/>RCommon Configuration</returns>
        private static IRCommonBuilder WithChangeTracking(this IRCommonBuilder config)
        {
            config.Services.AddTransient<IEventRouter, InMemoryTransactionalEventRouter>();
            config.Services.AddScoped<IEntityEventTracker, InMemoryEntityEventTracker>();
            return config;
        }



    }
}
