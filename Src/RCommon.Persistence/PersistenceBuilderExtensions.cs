using Microsoft.Extensions.DependencyInjection;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
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

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder) 
            where TObjectAccess: IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            return WithPersistence<TObjectAccess, TUnitOfWork>(builder, x => { }, x => { });
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder,
            Action<TObjectAccess> objectAccessActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {

            return WithPersistence<TObjectAccess, TUnitOfWork>(builder, objectAccessActions, x => { });
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder,
            Action<TUnitOfWork> uniOfWorkActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {

            return WithPersistence<TObjectAccess, TUnitOfWork>(builder, x => { }, uniOfWorkActions);
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder, 
            Action<TObjectAccess> objectAccessActions, Action<TUnitOfWork> unitOfWorkActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            // Data Store Management
            builder.Services.AddScoped<IScopedDataStore, ScopedDataStore>();
            builder.Services.AddScoped<IDataStoreRegistry, ScopedDataStoreRegistry>();

            // Object Access and Unit of Work Configurations 
            // Wire up the "out of the box" events/event handlers used in persistence. These are not transactional
            //builder.Services.AddScoped<ISubscriber<UnitOfWorkCreatedEvent>, UnitOfWorkCreatedHandler>();
            //builder.Services.AddScoped<ISubscriber<UnitOfWorkCommittedEvent>, UnitOfWorkCommittedEventHandler>();

            var dataConfiguration = (TObjectAccess)Activator.CreateInstance(typeof(TObjectAccess), new object[] { builder.Services });
            objectAccessActions(dataConfiguration);
            var unitOfWorkConfiguration = (TUnitOfWork)Activator.CreateInstance(typeof(TUnitOfWork), new object[] { builder.Services });
            unitOfWorkActions(unitOfWorkConfiguration);
            builder = WithEventTracking(builder);
            return builder;
        }


        /// <summary>
        /// Right now we are always using change tracking due to requirements for publishing entity events and those events being
        /// somewhat tied to Change Tracking.
        /// </summary>
        /// <param name="builder">Instance of <see cref="IRCommonBuilder"/>passed in.</param>
        /// <returns>Updated instance of <see cref="IRCommonBuilder"/>RCommon Configuration</returns>
        private static IRCommonBuilder WithEventTracking(this IRCommonBuilder builder)
        {
            builder.Services.AddScoped<IEventRouter, InMemoryTransactionalEventRouter>();
            builder.Services.AddScoped<IEntityEventTracker, InMemoryEntityEventTracker>();
            return builder;
        }



    }
}
