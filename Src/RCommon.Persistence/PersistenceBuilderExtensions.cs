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
        public static IRCommonBuilder WithPersistence<TObjectAccess>(this IRCommonBuilder builder)
            where TObjectAccess : IPersistenceBuilder
        {
            return WithPersistence<TObjectAccess>(builder, x => { });
        }

        public static IRCommonBuilder WithPersistence<TObjectAccess>(this IRCommonBuilder builder, Action<TObjectAccess> objectAccessActions)
            where TObjectAccess : IPersistenceBuilder
        {
            var dataConfiguration = (TObjectAccess)Activator.CreateInstance(typeof(TObjectAccess), new object[] { builder.Services });
            objectAccessActions(dataConfiguration);
            builder = WithEventTracking(builder);
            return builder;
        }

        public static IRCommonBuilder WithUnitOfWork<TUnitOfWork>(this IRCommonBuilder builder, Action<TUnitOfWork> unitOfWorkActions)
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            var unitOfWorkConfiguration = (TUnitOfWork)Activator.CreateInstance(typeof(TUnitOfWork), new object[] { builder.Services });
            unitOfWorkActions(unitOfWorkConfiguration);
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

        // Deprecated
        /// <summary>
        /// Deprecated. Use <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder)"></see> or <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder, Action{TObjectAccess})"/>
        /// and if unit of work is required then use in conjuction with <see cref="WithUnitOfWork{TUnitOfWork}(IRCommonBuilder, Action{TUnitOfWork})"/>
        /// </summary>
        /// <typeparam name="TObjectAccess"></typeparam>
        /// <typeparam name="TUnitOfWork"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        [Obsolete("This is deprecated as peristence is decoupled from unit of work.")]
        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder) 
            where TObjectAccess: IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            return WithPersistence<TObjectAccess, TUnitOfWork>(builder, x => { }, x => { });
        }

        /// <summary>
        /// Deprecated. Use <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder)"></see> or <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder, Action{TObjectAccess})"/>
        /// and if unit of work is required then use in conjuction with <see cref="WithUnitOfWork{TUnitOfWork}(IRCommonBuilder, Action{TUnitOfWork})"/>
        /// </summary>
        /// <typeparam name="TObjectAccess"></typeparam>
        /// <typeparam name="TUnitOfWork"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectAccessActions"></param>
        /// <returns></returns>
        [Obsolete("This is deprecated as peristence is decoupled from unit of work.")]
        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder,
            Action<TObjectAccess> objectAccessActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            return WithPersistence<TObjectAccess, TUnitOfWork>(builder, objectAccessActions, x => { });
        }

        /// <summary>
        /// Deprecated. Use <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder)"></see> or <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder, Action{TObjectAccess})"/>
        /// and if unit of work is required then use in conjuction with <see cref="WithUnitOfWork{TUnitOfWork}(IRCommonBuilder, Action{TUnitOfWork})"/>
        /// </summary>
        /// <typeparam name="TObjectAccess"></typeparam>
        /// <typeparam name="TUnitOfWork"></typeparam>
        /// <param name="builder"></param>
        /// <param name="uniOfWorkActions"></param>
        /// <returns></returns>
        [Obsolete("This is deprecated as peristence is decoupled from unit of work.")]
        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder,
            Action<TUnitOfWork> uniOfWorkActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            return WithPersistence<TObjectAccess, TUnitOfWork>(builder, x => { }, uniOfWorkActions);
        }

        /// <summary>
        /// Deprecated. Use <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder)"></see> or <see cref="WithPersistence{TObjectAccess}(IRCommonBuilder, Action{TObjectAccess})"/>
        /// and if unit of work is required then use in conjuction with <see cref="WithUnitOfWork{TUnitOfWork}(IRCommonBuilder, Action{TUnitOfWork})"/>
        /// </summary>
        /// <typeparam name="TObjectAccess"></typeparam>
        /// <typeparam name="TUnitOfWork"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectAccessActions"></param>
        /// <param name="unitOfWorkActions"></param>
        /// <returns></returns>
        [Obsolete("This is deprecated as peristence is decoupled from unit of work.")]
        public static IRCommonBuilder WithPersistence<TObjectAccess, TUnitOfWork>(this IRCommonBuilder builder, 
            Action<TObjectAccess> objectAccessActions, Action<TUnitOfWork> unitOfWorkActions)
            where TObjectAccess : IPersistenceBuilder
            where TUnitOfWork : IUnitOfWorkBuilder
        {
            var dataConfiguration = (TObjectAccess)Activator.CreateInstance(typeof(TObjectAccess), new object[] { builder.Services });
            objectAccessActions(dataConfiguration);
            var unitOfWorkConfiguration = (TUnitOfWork)Activator.CreateInstance(typeof(TUnitOfWork), new object[] { builder.Services });
            unitOfWorkActions(unitOfWorkConfiguration);
            builder = WithEventTracking(builder);
            return builder;
        }
    }
}
