

using System.Collections.Generic;
using System.Linq;
using RCommon.DependencyInjection;
using RCommon.Extensions;
using RCommon.ObjectAccess;
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

namespace RCommon.ObjectAccess.NHibernate
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
                var uow = this._unitOfWorkManager.CurrentUnitOfWork;
                RCommonSessionFactory factory;
                if (uow != null)
                {
                    factory = (RCommonSessionFactory)this._dataStoreProvider.GetDataStore(uow.TransactionId.Value, this.DataStoreName);
                    return factory.SessionFactory;

                }

                factory = (RCommonSessionFactory)this._dataStoreProvider.GetDataStore(this.DataStoreName);
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
        public override async Task AttachAsync(TEntity entity)
        {
            await SessionFactory.GetCurrentSession().UpdateAsync(entity);
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

        public override async Task AddAsync(TEntity entity)
        {
            await SessionFactory.GetCurrentSession().SaveOrUpdateAsync(entity);
        }

        public override async Task DeleteAsync(TEntity entity)
        {
            await SessionFactory.GetCurrentSession().DeleteAsync(entity);
        }

        public override async Task UpdateAsync(TEntity entity)
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

        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().AnyAsync(expression);
        }

        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification)
        {
            return await SessionFactory.GetCurrentSession().Query<TEntity>().AnyAsync(specification.Predicate);
        }

        public override Task DetachAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
