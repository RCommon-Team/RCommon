using RCommon.Application.DTO;
using System.Threading.Tasks;

namespace RCommon.Application.Services
{
    public interface ICrudAppService<TDataTransferObject>
    {
        CommandResult<TDataTransferObject> Create(TDataTransferObject dto);
        Task<CommandResult<bool>> CreateAsync(TDataTransferObject dto);
        CommandResult<bool> Delete(TDataTransferObject dto);
        Task<CommandResult<bool>> DeleteAsync(TDataTransferObject dto);
        CommandResult<TDataTransferObject> GetById(object primaryKey);
        Task<CommandResult<TDataTransferObject>> GetByIdAsync(object primaryKey);
        CommandResult<bool> Update(TDataTransferObject dto);
        Task<CommandResult<bool>> UpdateAsync(TDataTransferObject dto);
    }
}