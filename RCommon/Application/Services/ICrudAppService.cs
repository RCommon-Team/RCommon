using AutoMapper;
using RCommon.Application.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.Application.Services
{
    public interface ICrudAppService<TDataTransferObject>
    {
        Task<CommandResult<bool>> CreateAsync(TDataTransferObject dto);
        Task<CommandResult<bool>> DeleteAsync(TDataTransferObject dto);
        Task<CommandResult<TDataTransferObject>> GetByIdAsync(object primaryKey);
        Task<CommandResult<bool>> UpdateAsync(TDataTransferObject dto);
        Task<CommandResult<ICollection<TDataTransferObject>>> GetAllAsync();

        IMapper ObjectMapper { get; set; }
    }
}