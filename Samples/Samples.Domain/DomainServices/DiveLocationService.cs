using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.BusinessServices;
using RCommon.Collections;
using RCommon.DataServices.Transactions;
using RCommon.DataTransferObjects;
using RCommon.ExceptionHandling;
using RCommon.Expressions;
using RCommon.Extensions;
using RCommon.ObjectAccess;
using RCommon.Validation;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Domain.DomainServices
{
    /// <summary>
    /// This is a domain service for our <see cref="DiveLocation"/> domain object. It implements all of our business logic. It extends <see cref="CrudDomainService{TEntity}"/>. 
    /// Since our base class <see cref="CrudDomainService{TEntity}"/> already implements our CRUD in a best practices manner, all we need to do is add this logic for the operations that are 
    /// not as basic - such as paging, eager loading, etc. It is not explicitly aware of any transactions that may be occuring. 
    /// </summary>
    public class DiveLocationService : CrudBusinessService<DiveLocation>, IDiveLocationService
    {
        private readonly IFullFeaturedRepository<DiveLocationDetail> _diveLocationDetailRepository;
        private readonly IFullFeaturedRepository<DiveLocation> _diveLocationRepository;

        public DiveLocationService(IFullFeaturedRepository<DiveLocationDetail> diveLocationDetailRepository, IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IFullFeaturedRepository<DiveLocation> diveLocationRepository, ILogger<DiveLocationService> logger, IExceptionManager exceptionManager)
            : base(unitOfWorkScopeFactory, diveLocationRepository, logger, exceptionManager)
        {
            _diveLocationDetailRepository = diveLocationDetailRepository;
            _diveLocationRepository = diveLocationRepository;
            _diveLocationRepository.DataStoreName = DataStoreDefinitions.Samples;
            _diveLocationDetailRepository.DataStoreName = DataStoreDefinitions.Samples;
        }


        public override async Task<CommandResult<DiveLocation>> GetByIdAsync(object primaryKey)
        {
            var result = new CommandResult<DiveLocation>();
            try
            {
                if (primaryKey == null)
                {
                    result.ValidationResult.AddError(new ValidationError("Primary Key cannot be null", "primaryKey"));
                }

                if (result.ValidationResult.IsValid)
                {
                    _diveLocationRepository.EagerlyWith(x => x.DiveLocationDetail);
                    _diveLocationRepository.EagerlyWith(x => x.DiveType);
                    Guid id = (Guid)primaryKey;
                    result.DataResult = _diveLocationRepository.FirstOrDefault(x=>x.Id == id);
                    this.Logger.LogDebug("Getting entity of type {0} by Id: {1}.", typeof(DiveLocation), primaryKey);
                }
                else
                {
                    this.Logger.LogWarning("Input was not validated for GetByIdAsync method - primaryKey of {0}", primaryKey);
                }

            }
            catch (ApplicationException ex)
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);

            }

            return await Task.FromResult(result);
        }


        /// <summary>
        /// Retrieves a list of <see cref="DiveLocation"/>
        /// </summary>
        /// <param name="includeDetails">Flag for including Image and other details</param>
        /// <param name="pageIndex">Current index of paging</param>
        /// <param name="pageSize">Size of objects to include in paging</param>
        /// <returns>Returns an object called <see cref="CommandResult{TResult}"/> which encapsulates <see cref="IPaginatedList{T}"/> of type <see cref="DiveLocation"/></returns>
        /// <exception cref="BusinessException">BusinessException wraps all other exceptions.</exception>
        public async Task<CommandResult<IPaginatedList<DiveLocation>>> GetAllDiveLocationsAsync(bool includeDetails, int pageIndex, int pageSize)
        {
            var result = new CommandResult<IPaginatedList<DiveLocation>>();
            try
            {
                
                if (includeDetails)
                {
                    // This is how we handle eager loading
                    _diveLocationRepository.EagerlyWith(x => x.DiveType); 
                    _diveLocationRepository.EagerlyWith(x => x.DiveLocationDetail); 
                }

                var query = _diveLocationRepository.Where(x => true); // We are deferring execution by doing this.
                query = query.OrderBy(x => x.LocationName); // Sort by Name for now. We can add sorting criteria later.
                result.DataResult = query.ToPaginatedList(pageIndex, pageSize);
                this.Logger.LogDebug("Getting a paged list of Dive Locations of type {0}.", typeof(DiveLocation).Name);

                return await Task.FromResult(result);
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex; // Encapsulate the exception
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy); // check out what is happening under the hood by looking at the "EhabExceptionHandlingConfiguration" class
                
            }
            return result;
        }

        public async Task<CommandResult<IPaginatedList<DiveLocation>>> SearchDiveLocationsAsync(string searchTerms, bool includeDetails, int pageIndex, int pageSize)
        {
            var result = new CommandResult<IPaginatedList<DiveLocation>>();
            try
            {

                if (includeDetails)
                {
                    // This is how we handle eager loading
                    _diveLocationRepository.EagerlyWith(x => x.DiveType);
                    _diveLocationRepository.EagerlyWith(x => x.DiveLocationDetail);
                }

                var query = _diveLocationRepository.Where(x => x.LocationName.StartsWith(searchTerms) || x.DiveDesc.Contains(searchTerms)); // We are deferring execution by doing this.
                query = query.OrderBy(x => x.LocationName); // Sort by Name for now. We can add sorting criteria later.
                result.DataResult = query.ToPaginatedList(pageIndex, pageSize);
                this.Logger.LogDebug("Getting a paged list of Dive Locations of type {0}.", typeof(DiveLocation).Name);

                return await Task.FromResult(result);
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex; // Encapsulate the exception
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy); // check out what is happening under the hood by looking at the "EhabExceptionHandlingConfiguration" class

            }
            return result;
        }


        public async Task<CommandResult<bool>> DeleteDiveLocationDetailsAsync(DiveLocationDetail diveDetails)
        {
            var result = new CommandResult<bool>();
            try
            {
                await _diveLocationDetailRepository.DeleteAsync(diveDetails);
                this.Logger.LogInformation("Deleting Dive Location Details {0}.", diveDetails);
                result.DataResult = true;

            }
            catch (ApplicationException ex)
            {
                result.DataResult = false;
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);

            }
            return result;
        }

        public async Task<CommandResult<bool>> CreateDiveLocationDetailsAsync(DiveLocationDetail locationDetail)
        {
            var result = new CommandResult<bool>();
            try
            {
                await _diveLocationDetailRepository.AddAsync(locationDetail);
                this.Logger.LogInformation("Creating Dive Location Details {0}.", locationDetail);
                result.DataResult = true;

            }
            catch (ApplicationException ex)
            {
                result.DataResult = false;
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
            }
            return result;
        }

        public async Task<CommandResult<bool>> UpdateDiveLocationDetailsAsync(DiveLocationDetail locationDetail)
        {
            var result = new CommandResult<bool>();
            try
            {
                await _diveLocationDetailRepository.UpdateAsync(locationDetail);
                this.Logger.LogInformation("Updating Dive Location Details {0}.", locationDetail);
                result.DataResult = true;

            }
            catch (ApplicationException ex)
            {
                result.DataResult = false;
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
            }
            return result;
        }

    }
}
