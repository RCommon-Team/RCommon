using AutoMapper;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using RCommon.Models;
using Samples.Application.Contracts.Dto;
using Samples.Domain.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Application.ApplicationServices
{
    public class ApplicationUserService : RCommonAppService, IApplicationUserService
    {
        private readonly IUserService _userService;
        private readonly IMapper _objectMapper;

        public ApplicationUserService(IUserService userService, IMapper objectMapper, ILogger<ApplicationUserService> logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory unitOfWorkScopeFactory)
            : base(logger, exceptionManager, unitOfWorkScopeFactory)
        {
            _userService = userService;
            _objectMapper = objectMapper;
        }

        public async virtual Task<CommandResult<ApplicationUserListModel>> SearchUsersAsync(ApplicationUserSearchRequest request)
        {
            var cmd = new CommandResult<ApplicationUserListModel>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {

                var userCmd = await _userService.SearchUsersAsync(request);// Perform the work

                cmd.DataResult = new ApplicationUserListModel(userCmd.DataResult, request, userCmd.DataResult.TotalCount, userCmd.DataResult.PageSize);// Map the PaginatedList to a DTO

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex.Message, ex);
                throw;
            }
            return cmd;

        }
    }
}
