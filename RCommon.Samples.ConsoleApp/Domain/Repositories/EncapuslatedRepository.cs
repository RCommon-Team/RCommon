using Microsoft.EntityFrameworkCore;
using RCommon.Domain.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RCommon;

namespace RCommon.Samples.ConsoleApp.Domain.Repositories
{
    /// <summary>
    /// This class encapsulates logic for the provider specific repository classes downstream. It does not utilize any 
    /// persistance methods (either persimistic or optimistic) whereby no object tracking is implemented and the last object in wins. This class is intended to 
    /// represent the aggregate root for domain classes though the domain repository should only be expressed through 
    /// a concrete domain repository interface <code>public interface IOrderRepository : IEncapsulatedRepository<Order></code>.
    /// If an extension is required to implement additional functionality to a concrete domain repository then this class 
    /// may be used as a base class for the concrete repository. We ensure the DRY priciple by not exposing the IQuerable interface
    /// through this class but allowing us to utilize the querable interface for complex joins in our domain. IQueryable should never be
    /// exposed in any methods here.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EncapsulatedRepository<TEntity> : IEncapsulatedRepository<TEntity>
    {
        private IEagerFetchingRepository<TEntity> _repository;



        public EncapsulatedRepository(IEagerFetchingRepository<TEntity> repository)
        {

            _repository = repository;
            _repository.DataStoreName = "TestDbContext";
        }



        public TEntity Add(TEntity entity)
        {
            try
            {
                return _repository.Add(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while adding an entity.", ex);
            }
        }

        public async Task AddAsync(TEntity entity)
        {
            try
            {
                await _repository.AddAsync(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while adding an entity.", ex);
            }
        }

        public void Attach(TEntity entity)
        {
            try
            {
                _repository.Attach(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while attaching an entity.", ex);
            }
        }

        public void Delete(TEntity entity)
        {
            try
            {
                _repository.Delete(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while deleting an entity.", ex);
            }
        }

        public async Task DeleteAsync(TEntity entity)
        {
            try
            {
                await _repository.DeleteAsync(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while deleting an entity.", ex);
            }
        }

        public IEagerFetchingRepository<TEntity> EagerlyWith(Action<EagerFetchingStrategy<TEntity>> strategyActions)
        {
            try
            {
                return _repository.EagerlyWith(strategyActions);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while eager loading an entity.", ex);
            }
        }

        public IEagerFetchingRepository<TEntity> EagerlyWith(Expression<Func<TEntity, object>> path)
        {
            try
            {
                return _repository.EagerlyWith(path);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while eager loading an entity.", ex);
            }
        }

        public ICollection<TEntity> Find(ISpecification<TEntity> specification)
        {
            try
            {
                return _repository.Find(specification);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding an entity.", ex);
            }
        }

        public ICollection<TEntity> Find(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return _repository.Find(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding an entity.", ex);
            }
        }

        public TEntity Find(object primaryKey)
        {
            try
            {
                return _repository.Find(primaryKey);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding an entity.", ex);
            }
        }

        public async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification)
        {
            try
            {
                return await _repository.FindAsync(specification);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding an entity.", ex);
            }
        }

        public async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return await _repository.FindAsync(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding an entity.", ex);
            }
        }

        public async Task<TEntity> FindAsync(object primaryKey)
        {
            try
            {
                return await _repository.FindAsync(primaryKey);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding an entity.", ex);
            }
        }

        public TEntity FindSingleOrDefault(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return _repository.FindSingleOrDefault(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding single of an entity.", ex);
            }
        }

        public TEntity FindSingleOrDefault(ISpecification<TEntity> specification)
        {
            try
            {
                return _repository.FindSingleOrDefault(specification);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding single of an entity.", ex);
            }
        }

        public async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return await _repository.FindSingleOrDefaultAsync(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding single of an entity.", ex);
            }
        }

        public async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification)
        {
            try
            {
                return await _repository.FindSingleOrDefaultAsync(specification);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while finding single of an entity.", ex);
            }
        }

        public int GetCount(ISpecification<TEntity> selectSpec)
        {
            try
            {
                return _repository.GetCount(selectSpec);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while getting count on an entity.", ex);
            }
        }

        public int GetCount(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return _repository.GetCount(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while getting count on an entity.", ex);
            }
        }

        public async Task<int> GetCountAsync(ISpecification<TEntity> selectSpec)
        {
            try
            {
                return await _repository.GetCountAsync(selectSpec);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while getting count on an entity.", ex);
            }
        }

        public async Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return await _repository.GetCountAsync(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while getting count on an entity.", ex);
            }
        }

        public void Update(TEntity entity)
        {
            try
            {
                _repository.Update(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while updating an entity.", ex);
            }
        }

        public async Task UpdateAsync(TEntity entity)
        {
            try
            {
                await _repository.UpdateAsync(entity);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while updating an entity.", ex);
            }
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression)
        {
            try
            {
                return await _repository.AnyAsync(expression);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while updating an entity.", ex);
            }
        }

        public async Task<bool> AnyAsync(ISpecification<TEntity> selectSpec)
        {
            try
            {
                return await _repository.AnyAsync(selectSpec.Predicate);
            }
            catch (ApplicationException ex)
            {

                throw new RepositoryException("An error occured while updating an entity.", ex);
            }
        }
    }
}
