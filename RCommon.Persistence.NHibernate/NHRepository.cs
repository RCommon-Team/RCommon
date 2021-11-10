﻿

using System.Collections.Generic;
using System.Linq;
using RCommon.DependencyInjection;
using RCommon.Extensions;
using RCommon.Persistence;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using RCommon.DataServices.Transactions;
using RCommon.Expressions;
using System;
using System.Linq.Expressions;
using System.IO;
using System.Threading.Tasks;
using RCommon.DataServices;
using RCommon.BusinessEntities;
using System.Threading;

namespace RCommon.Persistence.NHibernate
{
    /// <summary>
    /// Inherits from the <see cref="FullFeaturedRepositoryBase{TEntity}"/> class to provide an implementation of a
    /// repository that uses NHibernate.
    /// </summary>
    public class NHRepository<TEntity> : FullFeaturedRepositoryBase<TEntity>, INHRepository<TEntity> 
        where TEntity : class, IBusinessEntity
    {
        //int _batchSize = -1;
        //bool _enableCached;
        //string _cachedQueryName;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IDataStoreProvider _dataStoreProvider;

        /// <summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="NHRepository{TEntity}"/> class.
        /// </summary>
        public NHRepository(IDataStoreProvider dataStoreProvider, IUnitOfWorkManager unitOfWorkManager)
        {
            this._dataStoreProvider = dataStoreProvider;
            this._unitOfWorkManager = unitOfWorkManager;
        }

        /// <summary>
        /// Gets the <see cref="ISession"/> instnace that is used by the repository.
        /// </summary>
        private ISessionFactory SessionFactory
        {
            get
            {
                
                RCommonSessionFactory factory;
                var uow = this._unitOfWorkManager.CurrentUnitOfWork;

                if (uow != null)
                {

                    factory = this._dataStoreProvider.GetDataStore<RCommonSessionFactory>(uow.TransactionId.Value, this.DataStoreName);
                    return factory.SessionFactory;
                }
                factory = this._dataStoreProvider.GetDataStore<RCommonSessionFactory>(this.DataStoreName);
                return factory.SessionFactory;
            }
        }

        /// <summary>
        /// Gets the <see cref="IQueryable{TEntity}"/> used by the <see cref="FullFeaturedRepositoryBase{TEntity}"/> 
        /// to execute Linq queries.
        /// </summary>
        /// <value>A <see cref="IQueryable{TEntity}"/> instance.</value>
        /// <remarks>
        /// Inheritors of this base class should return a valid non-null <see cref="IQueryable{TEntity}"/> instance.
        /// </remarks>
        protected override IQueryable<TEntity> RepositoryQuery
        {
            get
            {


                return SessionFactory.GetCurrentSession().Query<TEntity>();
            }
        }

        public override bool Tracking { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


        /// <summary>
        /// Attaches a detached entity, previously detached via the <see cref="IRepository{TEntity}.Detach"/> method.
        /// </summary>
        /// <param name="entity">The entity instance to attach back to the repository.</param>
        public override async Task AttachAsync(TEntity entity, CancellationToken token = default)
        {
            await SessionFactory.GetCurrentSession().UpdateAsync(entity, token);
            _dataStoreProvider.RemoveRegisteredDataStores(this.SessionFactory.GetType(), Guid.Empty); // Remove any instance of this type so a fresh instance is used next time
        }

        protected override void ApplyFetchingStrategy(Expression[] paths)
        {
            Guard.Against<ArgumentNullException>((paths == null) || (paths.Length == 0), "Expected a non-null and non-empty array of Expression instances representing the paths to eagerly load.");

            foreach (var item in paths)
            {
                var exp = (Expression<Func<TEntity, object>>)item;
                this.RepositoryQuery.Fetch(exp);
            }
        }


        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate);
        }

        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression);
        }

        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await SessionFactory.GetCurrentSession().SaveOrUpdateAsync(entity);
            _dataStoreProvider.RemoveRegisteredDataStores(this.SessionFactory.GetType(), Guid.Empty); // Remove any instance of this type so a fresh instance is used next time
        }

        public override async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            await SessionFactory.GetCurrentSession().DeleteAsync(entity, token);
            _dataStoreProvider.RemoveRegisteredDataStores(this.SessionFactory.GetType(), Guid.Empty); // Remove any instance of this type so a fresh instance is used next time
        }

        public override async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            await SessionFactory.GetCurrentSession().UpdateAsync(entity);
            _dataStoreProvider.RemoveRegisteredDataStores(this.SessionFactory.GetType(), Guid.Empty); // Remove any instance of this type so a fresh instance is used next time
        }

        public override async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate).ToListAsync(token);
        }

        public override async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).ToListAsync(token);
        }

        public override async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().GetAsync<TEntity>(primaryKey, token);
        }

        public override async Task<int> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(selectSpec.Predicate).CountAsync(token);
        }

        public override async Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).CountAsync(token);
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).SingleOrDefaultAsync(token);
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate).SingleOrDefaultAsync(token);
        }

        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().AnyAsync(expression, token);
        }

        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().AnyAsync(specification.Predicate, token);
        }

        public override Task DetachAsync(TEntity entity, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}