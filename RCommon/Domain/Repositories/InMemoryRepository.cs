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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RCommon.Domain.Repositories
{
    /// <summary>
    /// An implementation of <see cref="FullFeaturedRepositoryBase{TEntity}"/> that uses an inmemory
    /// collection.
    /// </summary>
    /// <typeparam name="TEntity">The entity type for which this repository was created.</typeparam>
    /// <remarks>This class can be used in Unit tests to represent an in memory repository.</remarks>
    public class InMemoryRepository<TEntity, TDataStore> : ICrudRepository<TEntity>, IQueryable<TEntity>
    {
        readonly IList<TEntity> _internal;

        /// <summary>
        /// Default Constructor.
        /// Creats a new instance of the <see cref="InMemoryRepository{TEntity}"/> class.
        /// </summary>
        /// <param name="list">An optional list pre-populated with entities.</param>
        public InMemoryRepository(IList<TEntity> list)
        {
            _internal = list ?? new List<TEntity>();
        }

        public Type ElementType => throw new NotImplementedException();

        public Expression Expression => throw new NotImplementedException();

        public IQueryProvider Provider => throw new NotImplementedException();



        /// <summary>
        /// Adds the entity instance to the in-memory collection.
        /// </summary>
        /// <param name="entity"></param>
        public TEntity Add(TEntity entity)
        {
            _internal.Add(entity);
            return entity;
        }

        public void Attach(TEntity entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Marks the entity instance to be deleted from the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> that should be deleted.</param>
        public void Delete(TEntity entity)
        {
            _internal.Remove(entity);
        }

        public ICollection<TEntity> Find(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public ICollection<TEntity> Find(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public TEntity Find(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public TEntity FindSingleOrDefault(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public TEntity FindSingleOrDefault(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public int GetCount(ISpecification<TEntity> selectSpec)
        {
            throw new NotImplementedException();
        }

        public int GetCount(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Update(TEntity entity)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public string DataStoreName { get; set; }
    }

}