#region license
//Copyright 2010 Ritesh Rao 

//Licensed under the Apache License, Version 2.0 (the "License"); 
//you may not use this file except in compliance with the License. 
//You may obtain a copy of the License at 

//http://www.apache.org/licenses/LICENSE-2.0 

//Unless required by applicable law or agreed to in writing, software 
//distributed under the License is distributed on an "AS IS" BASIS, 
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//See the License for the specific language governing permissions and 
//limitations under the License. 
#endregion

using RCommon.BusinessEntities;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess
{
    ///<summary>
    /// A base class for implementors of <see cref="IRepository{TEntity}"/>.
    ///</summary>
    ///<typeparam name="TEntity"></typeparam>
    public abstract class FullFeaturedRepositoryBase<TEntity> : DisposableResource, IFullFeaturedRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {


        /// <summary>
        /// Gets the <see cref="IQueryable{TEntity}"/> used by the <see cref="FullFeaturedRepositoryBase{TEntity}"/> 
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

        public string DataStoreName { get; set; }


        


        /// <summary>
        /// Attaches a detached entity, previously detached via the <see cref="IRepository{TEntity}.Detach"/> method.
        /// </summary>
        /// <param name="entity">The entity instance to attach back to the repository.</param>
        public abstract Task AttachAsync(TEntity entity);

        public abstract Task DetachAsync(TEntity entity);


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

        protected abstract void ApplyFetchingStrategy(Expression[] paths);

        public IEagerFetchingRepository<TEntity> EagerlyWith(Action<EagerFetchingStrategy<TEntity>> strategyActions)
        {
            EagerFetchingStrategy<TEntity> strategy = new EagerFetchingStrategy<TEntity>();
            strategyActions(strategy);
            this.ApplyFetchingStrategy(strategy.Paths.ToArray<Expression>());
            return this;
        }

        public IEagerFetchingRepository<TEntity> EagerlyWith(Expression<Func<TEntity, object>> path)
        {
            Expression<Func<TEntity, object>>[] expressionArray = new Expression<Func<TEntity, object>>[] { path };
            this.ApplyFetchingStrategy((Expression[])expressionArray);
            return this;

        }

        public abstract IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
        public abstract Task AddAsync(TEntity entity);
        public abstract Task DeleteAsync(TEntity entity);
        public abstract Task UpdateAsync(TEntity entity);
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<TEntity> FindAsync(object primaryKey);
        public abstract Task<int> GetCountAsync(ISpecification<TEntity> selectSpec);
        public abstract Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification);
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification);

        public abstract bool Tracking { get; set; }
    }
}