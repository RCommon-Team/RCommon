using AutoMapper;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices;
using RCommon.DataServices.Transactions;
using RCommon.DataTransferObjects;
using RCommon.ExceptionHandling;
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

        public async virtual Task<CommandResult<StaticPaginatedList<ApplicationUserDto>>> SearchUsersAsync(string searchTerms, int pageIndex, int pageSize)
        {
            var cmd = new CommandResult<StaticPaginatedList<ApplicationUserDto>>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {

                var userCmd = await _userService.SearchUsersAsync(searchTerms, pageIndex, pageSize);// Perform the work

                var userList = _objectMapper.Map<ICollection<ApplicationUserDto>>(userCmd.DataResult.OrderBy(x => x.LastName)); // I would normally write a custom type converter (see below) for this if time allowed

                cmd.DataResult = new StaticPaginatedList<ApplicationUserDto>()
                {
                    Data = userList,
                    PageIndex = userCmd.DataResult.PageIndex,
                    PageSize = userCmd.DataResult.PageSize,
                    TotalPages = userCmd.DataResult.TotalPages,
                    TotalCount = userCmd.DataResult.TotalCount,
                    HasNextPage = userCmd.DataResult.HasNextPage,
                    HasPreviousPage = userCmd.DataResult.HasPreviousPage
                };// Map the PaginatedList to a DTO

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
                throw ex;
            }
            return cmd;

        }
    }
}
