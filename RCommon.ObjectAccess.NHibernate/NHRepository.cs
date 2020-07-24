

using System.Collections.Generic;
using System.Linq;
using RCommon.DependencyInjection;
using RCommon.Extensions;
using RCommon.ObjectAccess;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using RCommon.Domain.Repositories;
using RCommon.DataServices.Transactions;
using RCommon.Expressions;
using System;
using System.Linq.Expressions;
using System.IO;
using System.Threading.Tasks;
using RCommon.DataServices;

namespace RCommon.ObjectAccess.NHibernate
{
    /// <summary>
    /// Inherits from the <see cref="FullFeaturedRepositoryBase{TEntity}"/> class to provide an implementation of a
    /// repository that uses NHibernate.
    /// </summary>
    public class NHRepository<TEntity, TDataStore> : FullFeaturedRepositoryBase<TEntity>
        where TEntity : class
    {
        //int _batchSize = -1;
        //bool _enableCached;
        //string _cachedQueryName;
         ISessionFactory _dataStore;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IDataStoreProvider _dataStoreProvider;

        /// <summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="NHRepository{TEntity}"/> class.
        /// </summary>
        public NHRepository (IDataStoreProvider dataStoreProvider, IUnitOfWorkManager unitOfWorkManager) 
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
                if (this._unitOfWorkManager.CurrentUnitOfWork != null)
                {

                    return this._dataStoreProvider.GetDataStore<RCommonSessionFactory>(this._unitOfWorkManager.CurrentUnitOfWork.TransactionId.Value).SessionFactory;

                }

                return this._dataStoreProvider.GetDataStore<RCommonSessionFactory>().SessionFactory;
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

        /// <summary>
        /// Adds a transient instance of <see cref="TEntity"/> to be tracked
        /// and persisted by the repository.
        /// </summary>
        /// <param name="entity"></param>
        /// <remarks>
        /// The Add method replaces the existing <see cref="FullFeaturedRepositoryBase{TEntity}.Save"/> method, which will
        /// eventually be removed from the public API.
        /// </remarks>
        public override TEntity Add(TEntity entity)
        {
            SessionFactory.GetCurrentSession().SaveOrUpdate(entity);
            return entity;
        }

        /// <summary>
        /// Marks the entity instance to be deleted from the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> that should be deleted.</param>
        public override void Delete(TEntity entity)
        {
            SessionFactory.GetCurrentSession().Delete(entity);
        }



        /// <summary>
        /// Attaches a detached entity, previously detached via the <see cref="IRepository{TEntity}.Detach"/> method.
        /// </summary>
        /// <param name="entity">The entity instance to attach back to the repository.</param>
        public override void Attach(TEntity entity)
        {
            SessionFactory.GetCurrentSession().Update(entity);
        }

        protected override void ApplyFetchingStrategy(Action<EagerFetchingStrategy<TEntity>> strategyActions)
        {
            EagerFetchingStrategy<TEntity> strategy = new EagerFetchingStrategy<TEntity>();
            strategyActions(strategy);
            var paths = strategy.Paths;

            foreach (var item in strategy.Paths)
            {
                var exp = (Expression < Func<TEntity, object> > ) item;
                this.RepositoryQuery.Fetch(exp);
            }

        }

        public override void Update(TEntity entity)
        {
            SessionFactory.GetCurrentSession().Update(entity);
        }

        public override ICollection<TEntity> Find(ISpecification<TEntity> specification)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate).ToList();
        }

        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate);
        }

        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression);
        }

        public override ICollection<TEntity> Find(Expression<Func<TEntity, bool>> expression)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).ToList();
        }

        public override TEntity Find(object primaryKey)
        {
            return SessionFactory.GetCurrentSession().Get<TEntity>(primaryKey);
        }

        public override int GetCount(ISpecification<TEntity> selectSpec)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(selectSpec.Predicate).Count();
        }

        public override int GetCount(Expression<Func<TEntity, bool>> expression)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).Count();
        }

        public override TEntity FindSingleOrDefault(Expression<Func<TEntity, bool>> expression)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).SingleOrDefault();
        }

        public override TEntity FindSingleOrDefault(ISpecification<TEntity> specification)
        {
            return SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate).SingleOrDefault();
        }

        public override async Task AddAsync(TEntity entity)
        {
            await SessionFactory.GetCurrentSession().SaveOrUpdateAsync(entity);
        }

        public override async Task DeleteAsync(TEntity entity)
        {
            await SessionFactory.GetCurrentSession().DeleteAsync(entity);
        }

        public override async Task UpdateAsnyc(TEntity entity)
        {
            await SessionFactory.GetCurrentSession().UpdateAsync(entity);
        }

        public override async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate).ToListAsync();
        }

        public override async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).ToListAsync();
        }

        public override async Task<TEntity> FindAsync(object primaryKey)
        {
            return await SessionFactory.GetCurrentSession().GetAsync<TEntity>(primaryKey);
        }

        public override async Task<int> GetCountAsync(ISpecification<TEntity> selectSpec)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(selectSpec.Predicate).CountAsync();
        }

        public override async Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).CountAsync();
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(expression).SingleOrDefaultAsync();
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().Where(specification.Predicate).SingleOrDefaultAsync();
        }
    }
}
