
using RCommon.Models;
using Samples.Application.Contracts.Dto;
using System.Threading.Tasks;

namespace Samples.Application.ApplicationServices
{
    public interface IApplicationUserService
    {
        Task<CommandResult<PaginatedListModel<ApplicationUserDto>>> SearchUsersAsync(string searchTerms, int pageIndex, int pageSize);
    }
}