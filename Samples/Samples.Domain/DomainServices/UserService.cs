using Microsoft.Extensions.Logging;
using RCommon.BusinessServices;
using RCommon.Collections;
using RCommon.ExceptionHandling;
using RCommon.Extensions;
using RCommon.Models;
using RCommon.Persistence;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Domain.DomainServices
{
    public class UserService : RCommonBusinessService, IUserService
    {
        private readonly IFullFeaturedRepository<ApplicationUser> _userRepository;

        public UserService(IFullFeaturedRepository<ApplicationUser> userRepository, ILogger<UserService> logger, IExceptionManager exceptionManager) : base(logger, exceptionManager)
        {
            _userRepository = userRepository;
            _userRepository.DataStoreName = DataStoreDefinitions.Samples;

        }


        public async Task<CommandResult<IPaginatedList<ApplicationUser>>> SearchUsersAsync(SearchPaginatedListRequest request)
        {
            var result = new CommandResult<IPaginatedList<ApplicationUser>>();
            try
            {


                var query = _userRepository.FindAsync(x => x.FirstName.StartsWith(request.SearchString) || x.LastName.StartsWith(request.SearchString),
                    x => x.LastName, true, request.PageNumber, request.PageSize.Value); // We are deferring execution by doing this.
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
