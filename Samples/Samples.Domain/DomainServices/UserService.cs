using Microsoft.Extensions.Logging;
using RCommon.Application.DTO;
using RCommon.Collections;
using RCommon.Domain.DomainServices;
using RCommon.Domain.Repositories;
using RCommon.ExceptionHandling;
using RCommon.Extensions;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Domain.DomainServices
{
    public class UserService : RCommonDomainService, IUserService
    {
        private readonly IEagerFetchingRepository<ApplicationUser> _userRepository;

        public UserService(IEagerFetchingRepository<ApplicationUser> userRepository, ILogger<UserService> logger, IExceptionManager exceptionManager) : base(logger, exceptionManager)
        {
            _userRepository = userRepository;
            _userRepository.DataStoreName = DataStoreDefinitions.Samples;

        }


        public async Task<CommandResult<IPaginatedList<ApplicationUser>>> SearchUsersAsync(string searchTerms, int pageIndex, int pageSize)
        {
            var result = new CommandResult<IPaginatedList<ApplicationUser>>();
            try
            {


                var query = _userRepository.Where(x => x.FirstName.StartsWith(searchTerms) || x.LastName.StartsWith(searchTerms)); // We are deferring execution by doing this.
                query = query.OrderBy(x => x.LastName); // Sort by Name for now. We can add sorting criteria later.
                result.DataResult = query.ToPaginatedList(pageIndex, pageSize);
                this.Logger.LogDebug("Getting a paged list of Application Users of type {0}.", typeof(ApplicationUser).Name);

                return await Task.FromResult(result);
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex; // Encapsulate the exception
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy); // check out what is happening under the hood by looking at the "EhabExceptionHandlingConfiguration" class

            }
            return result;
        }
    }
}
