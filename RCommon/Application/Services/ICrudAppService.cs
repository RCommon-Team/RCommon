using RCommon.Application.DTO;
using System.Threading.Tasks;

namespace RCommon.Application.Services
{
    public interface ICrudAppService<TDataTransferObject>
    {
        Task<CommandResult<bool>> CreateAsync(TDataTransferObject dto);
    }
}