
using RCommon.DataTransferObjects;
using Samples.Application.Contracts.Dto;
using System.Threading.Tasks;

namespace Samples.Application.ApplicationServices
{
    public interface IApplicationUserService
    {
        Task<CommandResult<StaticPaginatedList<ApplicationUserDto>>> SearchUsersAsync(string searchTerms, int pageIndex, int pageSize);
    }
}