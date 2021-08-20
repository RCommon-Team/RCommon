using AutoMapper;
using Microsoft.Extensions.Logging;
using RCommon.BusinessServices;
using RCommon.DataTransferObjects;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RCommon.BusinessEntities;

namespace RCommon.ApplicationServices
{
    public class CrudAppService<TDataTransferObject, TEntity> : RCommonAppService, ICrudAppService<TDataTransferObject>
        where TEntity : IBusinessEntity
    {
        private readonly ICrudBusinessService<TEntity> _crudDomainService;
        private IMapper _objectMapper;

        public CrudAppService(ICrudBusinessService<TEntity> crudDomainService, IMapper objectMapper, ILogger logger, IExceptionManager exceptionManager,
            IUnitOfWorkScopeFactory unitOfWorkScopeFactory)
            : base(logger, exceptionManager, unitOfWorkScopeFactory)
        {
            this._crudDomainService = crudDomainService;
            this._objectMapper = objectMapper;
        }

        public virtual async Task<CommandResult<bool>> CreateAsync(TDataTransferObject dto)
        {
            var result = new CommandResult<bool>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                var entity = this._objectMapper.Map<TEntity>(dto); // Map the entity to a DTO

                using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
                {
                    var domainData = await _crudDomainService.CreateAsync(entity); // Perform the work

                    if (domainData.HasException)
                    {

                        throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
                    }
                    else
                    {
                        result.DataResult = domainData.DataResult; // Set the data to return to the DTO
                    }

                    scope.Commit(); // Commit the transaction
                }

                return result;
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
                throw ex;
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

        }

        public virtual async Task<CommandResult<bool>> UpdateAsync(TDataTransferObject dto)
        {
            var result = new CommandResult<bool>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                var entity = this.ObjectMapper.Map<TEntity>(dto); // Map the entity to a DTO

                using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
                {
                    var domainData = await _crudDomainService.UpdateAsync(entity); // Perform the work

                    if (domainData.HasException)
                    {

                        throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
                    }
                    else
                    {
                        result.DataResult = domainData.DataResult; // Set the data to return to the DTO
                    }

                    scope.Commit(); // Commit the transaction
                }

                return result;
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
                throw ex;
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

        }

        public virtual async Task<CommandResult<bool>> DeleteAsync(TDataTransferObject dto)
        {
            var result = new CommandResult<bool>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                var entity = _objectMapper.Map<TEntity>(dto); // Map the entity to a DTO

                using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
                {
                    var domainData = await _crudDomainService.DeleteAsync(entity); // Perform the work

                    if (domainData.HasException)
                    {

                        throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
                    }
                    else
                    {
                        result.DataResult = domainData.DataResult; // Set the data to return to the DTO
                    }

                    scope.Commit(); // Commit the transaction
                }

                return result;
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
                throw ex;
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

        }

        public virtual async Task<CommandResult<TDataTransferObject>> GetByIdAsync(object primaryKey)
        {
            var result = new CommandResult<TDataTransferObject>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {

                var domainData = await _crudDomainService.GetByIdAsync(primaryKey); // Perform the work

                if (domainData.HasException)
                {

                    throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
                }
                else
                {
                    result.DataResult = _objectMapper.Map<TDataTransferObject>(domainData.DataResult); // Map the entity to a DTO
                }

                return result;
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
                throw ex;
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

        }

        public async virtual Task<CommandResult<ICollection<TDataTransferObject>>> GetAllAsync()
        {
            var result = new CommandResult<ICollection<TDataTransferObject>>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {

                var domainData = await _crudDomainService.GetAllAsync(); // Perform the work

                if (domainData.HasException)
                {

                    throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
                }
                else
                {
                    result.DataResult = _objectMapper.Map<ICollection<TDataTransferObject>>(domainData.DataResult); // Map the entity to a DTO
                }
                return result;
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
                throw ex;
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

        }


        public IMapper ObjectMapper { get => _objectMapper; set => _objectMapper = value; }
    }
}
