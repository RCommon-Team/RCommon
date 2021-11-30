
using RCommon.Models;
using Samples.Application.Contracts.Dto;
using System.Threading.Tasks;

namespace Samples.Application.ApplicationServices
{
    public interface IApplicationUserService
    {
        Task<CommandResult<ApplicationUserListModel>> SearchUsersAsync(ApplicationUserSearchRequest request);
    }
}
