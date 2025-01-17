﻿using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RCommon.Entities;
using System.Data;
using Microsoft.Extensions.Logging;
using RCommon.Persistence.Sql;
using System.Threading;
using RCommon.Collections;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Crud
{
    public abstract class LinqRepositoryBase<TEntity> : DisposableResource, ILinqRepository<TEntity>
       where TEntity : IBusinessEntity
    {
        private string _dataStoreName;
        private readonly IDataStoreFactory _dataStoreFactory;

        public LinqRepositoryBase(IDataStoreFactory dataStoreFactory, 
            IEntityEventTracker eventTracker, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
        {
            if (defaultDataStoreOptions is null)
            {
                throw new ArgumentNullException(nameof(defaultDataStoreOptions));
            }
            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
            EventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));

            if (defaultDataStoreOptions != null && defaultDataStoreOptions.Value != null
                && !defaultDataStoreOptions.Value.DefaultDataStoreName.IsNullOrEmpty())
            {
                this.DataStoreName = defaultDataStoreOptions.Value.DefaultDataStoreName;
            }
        }


        /// <summary>
        /// Gets the <see cref="IQueryable{TEntity}"/> used by the <see cref="GraphRepositoryBase{TEntity}"/> 
        /// to execute Linq queries.
        /// </summary>
        /// <value>A <see cref="IQueryable{TEntity}"/> instance.</value>
        /// <remarks>
        /// Inheritors of this base class should return a valid non-null <see cref="IQueryable{TEntity}"/> instance.
        /// </remarks>
        protected abstract IQueryable<TEntity> RepositoryQuery { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{TEntity}" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return RepositoryQuery.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return RepositoryQuery.GetEnumerator();
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of <see cref="IQueryable" />.
        /// </summary>
        /// <returns>
        /// The <see cref="Expression" /> that is associated with this instance of <see cref="IQueryable" />.
        /// </returns>
        public Expression Expression
        {
            get { return RepositoryQuery.Expression; }
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="IQueryable" /> is executed.
        /// </summary>
        /// <returns>
        /// A <see cref="Type" /> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.
        /// </returns>
        public Type ElementType
        {
            get { return RepositoryQuery.ElementType; }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        /// <returns>
        /// The <see cref="IQueryProvider" /> that is associated with this data source.
        /// </returns>
        public IQueryProvider Provider
        {
            get { return RepositoryQuery.Provider; }
        }


        /// <summary>
        /// Querries the repository based on the provided specification and returns results that
        /// are only satisfied by the specification.
        /// </summary>
        /// <param name="specification">A <see cref="ISpecification{TEntity}"/> instnace used to filter results
        /// that only satisfy the specification.</param>
        /// <returns>A <see cref="IEnumerable{TEntity}"/> that can be used to enumerate over the results
        /// of the query.</returns>
        public IEnumerable<TEntity> Query(ISpecification<TEntity> specification)
        {
            return RepositoryQuery.Where(specification.Predicate).AsQueryable();
        }

        public abstract IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending);
        public abstract Task AddAsync(TEntity entity, CancellationToken token = default);
        public abstract Task DeleteAsync(TEntity entity, CancellationToken token = default);
        public abstract Task UpdateAsync(TEntity entity, CancellationToken token = default);
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);
        public abstract Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);
        public abstract Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        public abstract Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0,
            CancellationToken token = default);
        public abstract Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);

        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0);
        public abstract IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification);

        public abstract IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path);

        public abstract IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path);
        public ILogger Logger { get; set; }
        public IEntityEventTracker EventTracker { get; }
        public string DataStoreName
        {
            get => _dataStoreName;
            set
            {
                _dataStoreName = value;
            }
        }
    }

}
