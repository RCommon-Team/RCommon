
using RCommon.Collections;
using RCommon.Models;
using Samples.Domain.Entities;
using System.Threading.Tasks;

namespace Samples.Domain.DomainServices
{
    public interface IUserService
    {
        Task<CommandResult<IPaginatedList<ApplicationUser>>> SearchUsersAsync(SearchPaginatedListRequest request);
    }
}
