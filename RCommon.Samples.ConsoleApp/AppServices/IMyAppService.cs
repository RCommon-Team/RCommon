using RCommon.Application.DTO;
using RCommon.Application.Services;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Samples.ConsoleApp.Shared.Dto;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.AppServices
{
    public interface IMyAppService : ICrudAppService<CustomerDto>
    {
        Task<CommandResult<bool>> NewCustomerSignupPromotion(CustomerDto customerDto);
    }
}