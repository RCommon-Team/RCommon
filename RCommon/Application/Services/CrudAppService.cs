using AutoMapper;
using Microsoft.Extensions.Logging;
using RCommon.Application.DTO;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using Reactor2.CMS.DomainServices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Application.Services
{
    public class CrudAppService<TDataTransferObject, TEntity> : RCommonAppService, ICrudAppService<TDataTransferObject>
        where TEntity : class
    {
        private readonly ICrudDomainService<TEntity> _crudDomainService;
        private readonly IMapper _objectMapper;

        public CrudAppService(ICrudDomainService<TEntity> crudDomainService, IMapper objectMapper, ILogger logger, IExceptionManager exceptionManager,
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
                var entity = _objectMapper.Map<TEntity>(dto); // Map the entity to a DTO

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

                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }

        }
    }
}
